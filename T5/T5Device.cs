using System;

namespace DgusDude.T5
{
    using Core;
    public class T5Device : Device
    {
        private static byte[] T5_ACK_VALUE = new byte[] { 0x4F, 0x4B };
        public T5Device(Platform platform, Screen screen, uint? flashSize) 
            : base(platform, screen)
        {
            Config.AckValue = T5_ACK_VALUE;
            var platMask = platform & Platform.PlatformMask;
            var addressMode = AddressMode.Size2 | AddressMode.Shift1;
            switch (platform & Platform.ProcessorMask) {
                default: throw new ArgumentOutOfRangeException("processor");
                case Platform.T5:
                    Registers = new MemoryDirectAccessor(this, 0x900, 1, addressMode, 0x81, 0x80, 0x100); //2k Registers
                    RAM = new MemoryDirectAccessor(this, 0x20000, 2, addressMode | AddressMode.ShiftRead, 0x83, 0x82); //128kb
                    UserSettings = new NorAccessor(this, 0x50000); //320kb 3FFF0
                    break;
                case Platform.T5L:
                    Registers = new MemoryDirectAccessor(this, 0x900, 1, addressMode, 0x81, 0x80, 0x100); //2k Registers
                    RAM = new MemoryDirectAccessor(this, 0x20000, 2, addressMode | AddressMode.ShiftRead, 0x83, 0x82); //128kb
                    UserSettings = new NorAccessor(this, 0x50000); //320kb 3FFF0
                    break;
            }
            VP = new VP(RAM);
            Buffer = new MemoryBuffer(RAM, 0x10000, 0x10000);
            ADC = new ADC(this);
            if (!flashSize.HasValue) flashSize = 0x04000000; //64MB
            if (platMask == Platform.UID2)
                Storage = new NandAccessor(this, flashSize.Value, 0x020000/*128Kb*/, 0x8000 /*32kb*/); //64MB
            else {
                Storage = new NandAccessor(this, flashSize.Value, 0x040000/*256Kb*/, 0x8000 /*32kb*/); //64MB
                PWM = new PWM(this);
            } 
            Pictures = new PictureStorage(this, 240);
            Music = new MusicStorage(this, (flashSize.Value >> 1) / 0x020000/*128Kb*/, flashSize.Value >> 1 /*last half*/);
        }

        public ADC ADC { get; private set; }
        public PWM PWM { get; private set; }

        public override void Reset(bool cpuOnly = false) => VP.Write(0x04, new byte[] { 0x55, 0xAA, 0x5A, (byte)(cpuOnly ? 0xA5 : 0x5A) });
        public DeviceInfo GetDeviceInfo() => new DeviceInfo(this);
        public SystemConfig GetDeviceConfig(bool refresh = true) => new SystemConfig(this, refresh);
        public LCDBrightness GetBrightness(bool refresh = true) => new LCDBrightness(this, refresh);

        protected class T5TouchStatus : TouchStatus
        {
            public T5TouchStatus(Device device, bool refresh = true) : base(device, 0x16, 8, refresh) { }
            public override void Simulate(TouchAction action, uint x, uint y)
            {
                var val = new byte[8] { 0x5A, 0xA5, 00, (byte)action, 0, 0, 0, 0 };
                ((int)x).ToLittleEndien(val, 4, 2);
                ((int)y).ToLittleEndien(val, 6, 2);
                Memory.Write(0xD4, new ArraySegment<byte>(val));
                (Memory as VP).Wait(0xD4);
            }
        }
        public override TouchStatus GetTouch()
        {
            if ((Platform & Platform.TouchScreen) == 0) return base.GetTouch();
            return new T5TouchStatus(this);
        }

        public void UploadOS(byte[] data, byte target = 0x10, bool verify = false)
        {
            var offset = target == 0x10 ? 0x1000 : 0; //DWIN cache org
            var bufSize = (target == 0x10 ? 0x7000 /*28kb*/: 0x10000/*64kb*/) - offset;
            if (bufSize > Buffer.Length) throw new System.Exception("buffer size too small");
            if (data.Length > bufSize) throw new System.Exception("file too big to upload");
            var newBufffer = new byte[bufSize];
            Array.Copy(data, offset, newBufffer, 0, data.Length - offset);
            //for (var i = data.Length - offset; i < newBufffer.Length; i++) newBufffer[i] = byte.MaxValue;
            RAM.Write(Buffer.Address, new ArraySegment<byte>(newBufffer), verify);
            var sramAddress = (Buffer.Address >> 1).ToLittleEndien(2);
            VP.Write(0x06, new byte[] {
                0x5A, //fixed
                target, // 0x10: DWIN OS user code - 28kb, 0x5A: 8051 code - 64kb
                sramAddress[0], sramAddress[1], //SRAM position
            });
            System.Threading.Thread.Sleep(200); //minimum wait time
            VP.Wait(0x06);
        }
        protected override void UploadBin(int fileIndex, byte[] data, bool verify = false)
        {
            if (fileIndex < 0 || fileIndex > 127) throw Exception.CreateOutOfRange(fileIndex, 127);
            var address = (int)(fileIndex * (Storage as NandAccessor).BlockSize); //256kb blocks
            Storage.Write(address, new ArraySegment<byte>(), verify);
        }
        public override bool Upload(string fileName, bool verify = false)
        {
            if (fileName.StartsWith("T5", StringComparison.InvariantCultureIgnoreCase))
                return false; //skip - unable to upload it
            if (fileName.StartsWith("DWINOS", StringComparison.InvariantCultureIgnoreCase))
            {
                UploadOS(System.IO.File.ReadAllBytes(fileName), 0x10);
                return true;
            }
            return base.Upload(fileName, verify);
        }

        public override void Format(Action<int> progress = null)
        {
            Buffer.Clear(Storage.PageSize);

        }
    }
}
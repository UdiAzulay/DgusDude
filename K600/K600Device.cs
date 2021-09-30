using System;
namespace DgusDude.K600
{
    using Core;
    public class K600Device : Device
    {
        public K600Device(Platform platform, Screen screen, uint? flashSize) : base(platform, screen)
        {
            Registers = new MemoryDirectAccessor(this, 0x100, 1, AddressMode.Size1, 0x81, 0x80); //128 Registers
            RAM = new MemoryDirectAccessor(this, 0x7000, 2, AddressMode.Size2 | AddressMode.Shift1 | AddressMode.ShiftRead, 0x83, 0x82); //128kb

            VP = new VP(Registers);
            Buffer = new MemoryBuffer(RAM, 0x10000, 0x10000);

            if (!flashSize.HasValue) flashSize = 0x40000;
            Storage = new NandAccessor(this, flashSize.Value, 2, 0x40000);
            
            Pictures = new PictureStorage(this, (flashSize.Value >> 1) / screen.FrameSize);
            Music = new MusicStorage(this, (flashSize.Value >> 1) / 0x20000 /*128k*/);
        }

        public override void Reset(bool cpuOnly) => VP.Write(0xEE, new byte[] { 0x5A, 0xA5 });        

        public DeviceInfo GetDeviceInfo() => new DeviceInfo(this);
        public override TouchStatus GetTouch()
        {
            if ((Platform & Platform.TouchScreen) == 0) return base.GetTouch();
            return new TouchStatus(this, 0x05, 7);
        }

        public override void Format(Action<int> progress = null) {
            throw new NotImplementedException();
        }

        protected override void UploadBin(int libIndex, byte[] data, bool verify = false) {
            throw new NotImplementedException();
        }
    }
}

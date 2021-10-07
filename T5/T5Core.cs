using System;
using System.IO;
using System.Linq;

namespace DgusDude.T5
{
    using Core;
    public abstract class T5Core : Device
    {
        private static byte[] T5_ACK_VALUE = new byte[] { 0x4F, 0x4B };
        protected T5Core(Platform platform, Screen screen)
            : base(platform, screen)
        {
            Config.AckValue = T5_ACK_VALUE;
            var addressMode = AddressMode.Size2 | AddressMode.Shift1;
            Registers = new MemoryDirectAccessor(this, 0x900, 1, addressMode, 0x81, 0x80, 0x100); //2k Registers
            RAM = new MemoryDirectAccessor(this, 0x20000, 2, addressMode | AddressMode.ShiftRead, 0x83, 0x82); //128kb
            UserSettings = new NorAccessor(this, 0x50000); //320kb 3FFF0
            Buffer = new MemoryBuffer(RAM, (int)(RAM.Length >> 1), RAM.Length >> 1);
            VP = new VP(RAM);
        }

        public override void Reset(bool cpuOnly = false) => VP.Write(0x04, new byte[] { 0x55, 0xAA, 0x5A, (byte)(cpuOnly ? 0xA5 : 0x5A) });
        
        protected void Upload_OS(Stream stream, byte target = 0x10, bool verify = false)
        {
            var offset = target == 0x10 ? 0x1000 : 0; //DWIN cache org
            //var maxLength = (target == 0x10 ? 0x7000 /*28kb*/: 0x10000/*64kb*/) - offset;
            stream.Seek(offset, SeekOrigin.Current);
            RAM.Write(Buffer.Address, stream, verify);
            var sramAddress = (Buffer.Address >> 1).ToLittleEndien(2);
            VP.Write(0x06, new byte[] {
                0x5A, //fixed
                target, // 0x10: DWIN OS user code - 28kb, 0x5A: 8051 code - 64kb
                sramAddress[0], sramAddress[1], //SRAM position
            });
            System.Threading.Thread.Sleep(200); //minimum wait time
            VP.Wait(0x06);
        }

        public override string UploadExtensions => base.UploadExtensions + "HZK;DZK;BIN;ICO;LIB;DWINOS;";

        public override void Upload(Stream stream, string fileExt, int? index, bool verify = false)
        {
            if (new[] { "HZK", "DZK", "BIN", "ICO" }.Contains(fileExt)) {
                Upload(Storage, 0x40000 /*256kb*/, stream, index.Value, verify);
            } else if (fileExt.Equals("LIB")) {
                Upload(UserSettings, 0x1000, stream, index.Value, verify);
            } else if (fileExt.Equals("DWINOS")) { 
                Upload_OS(stream, 0x10);
            } else base.Upload(stream, fileExt, index, verify);
        }

        public override void Upload(string fileName, bool verify = false)
        {
            if (fileName.StartsWith("T5", StringComparison.InvariantCultureIgnoreCase))
                return; //skip - unable to upload it
            if (fileName.StartsWith("DWINOS", StringComparison.InvariantCultureIgnoreCase)) {
                using (var f = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    Upload(f, "DWINOS", null, verify);
            }
            base.Upload(fileName, verify);
        }

        public SystemConfig GetDeviceConfig(bool refresh = true) => new SystemConfig(this, refresh);
        public LCDBrightness GetBrightness(bool refresh = true) => new LCDBrightness(this, refresh);
    }
}
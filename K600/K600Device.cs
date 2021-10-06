using System;
namespace DgusDude.K600
{
    using Core;
    using System.IO;

    public class K600Device : Device
    {
        public K600Device(Platform platform, Screen screen, uint? flashSize = null) : base(platform, screen)
        {
            Registers = new MemoryDirectAccessor(this, 0x100, 1, AddressMode.Size1, 0x81, 0x80); //256 Registers
            var ramSize = (Platform & Platform.ProcessorMask) == Platform.K600Mini ? 0x1000 /*4kb*/ : 0xDFFE /*28kw*/;
            RAM = new MemoryDirectAccessor(this, (uint)ramSize, 2, AddressMode.Size2 | AddressMode.Shift1 | AddressMode.ShiftRead, 0x83, 0x82);
            VP = new VP(Registers);
            Buffer = new MemoryBuffer(RAM, (int)(RAM.Length >> 1), RAM.Length >> 1);

            if (!flashSize.HasValue) flashSize = 0x2000000; //32mb
            Storage = new NandAccessor(this, flashSize.Value, 4, 0x40000 /*128kw*/);
            
            Pictures = new PictureStorage(this, (flashSize.Value >> 1) / screen.FrameSize);
            Music = new MusicStorage(this, (flashSize.Value >> 1) / 0x20000 /*128kb*/);
            if ((Platform & Platform.TouchScreen) != 0) Touch = new Touch(this);
        }

        public override void Reset(bool cpuOnly) => VP.Write(0xEE, new byte[] { 0x5A, 0xA5 });        
        public DeviceInfo GetDeviceInfo() => new DeviceInfo(this);

        public override bool Upload(Stream stream, string fileExt, int? index, bool verify = false)
        {
            if (fileExt == "BMP") {
                using (var bmp = new System.Drawing.Bitmap(stream))
                    using (var ms = new MemoryStream(bmp.GetBytes(System.Drawing.Imaging.PixelFormat.Format24bppRgb)))
                        Upload(Storage, Screen.Width * Screen.Height * 3, ms, index.Value, verify);
                return true;
            }
            return base.Upload(stream, fileExt, index, verify);
        }

    }
}

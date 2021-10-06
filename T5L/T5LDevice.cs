using System;

namespace DgusDude.T5L
{
    using T5;
    public class T5LDevice : T5Core
    {
        public T5LDevice(Platform platform, Core.Screen screen, uint? flashSize = null, uint? musicOffset = null)
            : base(platform, screen)
        {
            if (!flashSize.HasValue) flashSize = 0x01000000;//16Mb, 0x04000000; //64MB
            if (!musicOffset.HasValue) musicOffset = flashSize.Value >> 1;
            Storage = new NandAccessor(this, flashSize.Value, 0x40000 /*256kb*/, 0x8000 /*32kb*/); //64MB
            Pictures = new PictureStorage(this, musicOffset.Value / Storage.PageSize);
            Music = new MusicStorage(this, (flashSize.Value  - musicOffset.Value) / 0x020000/*128Kb*/, musicOffset.Value);
            if ((Platform & Platform.TouchScreen) != 0) Touch = new Touch(this);
            PWM = new PWM(this, 1);
            ADC = new ADC(this, 8);
        }

        public DeviceInfo GetDeviceInfo() => new DeviceInfo(this);

        public override bool Upload(System.IO.Stream stream, string fileExt, int? index, bool verify = false)
        {
            switch (fileExt)
            {
                case "JPG":
                case "ICL":
                    Upload(Storage, 0x40000/*256kb*/, stream, index.Value, verify); 
                    return true;
                case "BMP": return false;
            }
            return base.Upload(stream, fileExt, index, verify);
        }
    }
}

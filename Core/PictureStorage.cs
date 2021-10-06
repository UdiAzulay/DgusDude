using System;

namespace DgusDude.Core
{
    public abstract class PictureStorage
    {
        protected readonly Device Device;
        public uint Length { get; private set; }
        public System.Drawing.Imaging.Encoder[] SupportedEncoders { get; private set; }
        public override string ToString() => string.Format("Max {0} items", Length);
        public PictureStorage(Device device, uint length) { Device = device; Length = length; }

        public abstract int Current { get; set; }
        //public abstract void UploadPicture(int pictureId, System.IO.Stream stream, System.Drawing.Imaging.ImageFormat format, bool verify);
        public virtual void TakeScreenshot(int index) { throw new NotImplementedException(); }
        public virtual void UploadPicture(int index, System.IO.Stream stream, System.Drawing.Imaging.ImageFormat imageFormat, bool verify = false)
        {
            string fileExt = null;
            if (imageFormat == System.Drawing.Imaging.ImageFormat.Jpeg) fileExt = "JPG";
            else if (imageFormat == System.Drawing.Imaging.ImageFormat.Bmp) fileExt = "BMP";
            else throw new ArgumentOutOfRangeException("imageFormat");
            Device.Upload(stream, fileExt, index, verify);
        }
    }
}

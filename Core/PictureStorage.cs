using System;

namespace DgusDude.Core
{
    public abstract class PictureStorage
    {
        public readonly Device Device;
        public uint Length { get; private set; }
        public System.Drawing.Imaging.Encoder[] SupportedEncoders { get; private set; }
        public override string ToString() { return string.Format("Max {0} items", Length); }
        public PictureStorage(Device device, uint length)
        {
            Device = device; Length = length;
        }

        protected static void SwapFileBytes(byte[] data, int startIndex, int length)
        {
            byte tmp;
            for (var i = startIndex; i < (startIndex + length); i += 2)
            {
                tmp = data[i]; data[i] = data[i + 1]; data[i + 1] = tmp;
            }
        }

        public abstract int Current { get; set; }
        public abstract void UploadPicture(int pictureId, System.IO.Stream stream, System.Drawing.Imaging.ImageFormat format, bool verify);
        public virtual void TakeScreenshot(int pictureId) { throw new NotImplementedException(); }

    }
}

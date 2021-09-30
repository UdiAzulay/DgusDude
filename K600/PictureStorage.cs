using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DgusDude.K600
{
    public class PictureStorage : Core.PictureStorage //, Core.IDrawPicture
    {
        public PictureStorage(Device device, uint length)
            : base(device, length) { }


        private void Upload_Bitmap(int pictureId, byte[] data, bool swapBytes = false, bool verify = false)
        {
            uint offset = 0;
            uint pictureSize = 0;//height * width * resolution
            if (swapBytes) SwapFileBytes(data, 0, data.Length);
            foreach (var v in new Core.Slicer(new ArraySegment<byte>(data), Device.Buffer.Length))
            {
                Device.Storage.Write((int)(pictureId * pictureSize), v, verify);
                offset += (uint)v.Count;
            }
        }

        public override int Current
        {
            get { return (int)Device.VP.Read(0x03, 2).FromLittleEndien(0, 2); }
            set
            {
                if (value > Length) throw DWINException.CreateOutOfRange(value, Length);
                var picIdBytes = value.ToLittleEndien(2);
                Device.VP.Write(0x03, picIdBytes);
            }
        }

        public override void UploadPicture(int pictureId, System.IO.Stream stream, ImageFormat format, bool verify)
        {
            if (pictureId > Length) throw DWINException.CreateOutOfRange(pictureId, Length);
            if (format == ImageFormat.Bmp) {
                using (var image = Image.FromStream(stream))
                    Upload_Bitmap(pictureId, new Bitmap(image).GetBytes(PixelFormat.Format16bppRgb565), true, verify);
            }
        }
    }
}

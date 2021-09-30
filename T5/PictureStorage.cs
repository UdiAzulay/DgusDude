using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DgusDude.T5
{
    public class PictureStorage : Core.PictureStorage //, Core.IDrawPicture
    {
        public PictureStorage(Device device, uint length) 
            : base(device, length) { }

        //upload 16 bit per pixel data
        private void Upload_Bitmap(byte[] data, bool swapBytes = false, bool verify = false)
        {
            var offset = 0;
            var sramAddress = (Device.Buffer.Address >> 1).ToLittleEndien(2);
            if (swapBytes) SwapFileBytes(data, 0, data.Length);
            foreach (var v in new Core.Slicer(new ArraySegment<byte>(data), Device.Buffer.Length))
            {
                Device.Buffer.Write(v, verify);
                var dataLength = (v.Count >> 1).ToLittleEndien(2);
                var imagePosition = (offset >> 1).ToLittleEndien(3);
                Device.VP.Write(0xA2, new byte[] {
                        0x5A, //fixed
                        sramAddress[0], sramAddress[1], //SRAM position
                        dataLength[0], dataLength[1], //data length in words
                        imagePosition[0], imagePosition[1], imagePosition[2] //image buffer position
                    });
                Device.VP.Wait(0xA2);
                offset += v.Count;
            }
        }

        private void Upload_Jpg(byte[] data, bool modeSave, ushort modeParam, bool verify = false)
        {
            if (data.Length > Device.Buffer.Length) throw new Exception("buffer size is too small, use BufferLength to enlarge it");
            var sramAddress = (Device.Buffer.Address >> 1).ToLittleEndien(2);
            Device.Buffer.Write(new ArraySegment<byte>(data), verify);
            var picIdOrPos = ((int)modeParam).ToLittleEndien(2);
            Device.VP.Write(0xA6, new byte[] {
                    0x5A, //fixed
                    (byte)(modeSave ? 0x02 : 0x01), //display
                    sramAddress[0], sramAddress[1], //sram position
                    picIdOrPos[0], picIdOrPos[1], //screen position or pictureId
                    0, 0 //undocumented, set 0
                });
            Device.VP.Wait(0xA6);
        }

        public override int Current
        {
            get { return (int)Device.VP.Read(0x14, 2).FromLittleEndien(0, 2); }
            set
            {
                if (value > Length) throw DWINException.CreateOutOfRange(value, Length);
                var picIdBytes = value.ToLittleEndien(2);
                Device.VP.Write(0x84, new byte[] { 0x5A, 0x01, picIdBytes[0], picIdBytes[1] });
                Device.VP.Wait(0x84);
            }
        }

        public override void TakeScreenshot(int pictureId)
        {
            if (pictureId > Length) throw DWINException.CreateOutOfRange(pictureId, Length);
            var picIdBytes = pictureId.ToLittleEndien(2);
            Device.VP.Write(0x84, new byte[] {
                    0x5A, //fixed
                    0x02, //save picture
                    picIdBytes[0], picIdBytes[1]
                });
        }

        public override void UploadPicture(int pictureId, System.IO.Stream stream, ImageFormat format, bool verify)
        {
            if (pictureId > Length) throw DWINException.CreateOutOfRange(pictureId, Length);
            if (format == ImageFormat.Bmp)
            {
                using (var image = Image.FromStream(stream))
                    Upload_Bitmap(new Bitmap(image).GetBytes(PixelFormat.Format16bppRgb565), true, verify);
                TakeScreenshot(pictureId);
            }
            else if (format == ImageFormat.Jpeg)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Upload_Jpg(data, true, (ushort)pictureId, verify);
            }
        }
    }
}

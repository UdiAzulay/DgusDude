using System;
using System.IO;
using System.Linq;

namespace DgusDude.T5
{
    using Core;
    public class T5Device : T5Core
    {
        public T5Device(Platform platform, Screen screen, uint? flashSize = null)
            : base(platform, screen)
        {
            if (!flashSize.HasValue) flashSize = 0x04000000; //64MB
            var musicOffset = flashSize.Value >> 1;
            Storage = (platform & Platform.PlatformMask) == Platform.UID2 ?
                new NandAccessor(this, flashSize.Value, 0x040000 /*128kw*/, 0) :
                new NandAccessor(this, flashSize.Value, 0x080000 /*256kw*/, 0x8000 /*32kb*/);
            Pictures = new PictureStorage(this, musicOffset / Storage.PageSize);
            Music = new MusicStorage(this, (flashSize.Value - musicOffset) / 0x020000/*128Kb*/, musicOffset);
            if ((Platform & Platform.TouchScreen) != 0) Touch = new Touch(this);
            switch (platform & Platform.PlatformMask)
            {
                case Platform.UID1:
                    PWM = new PWM(this, 3); //only on T5UID1
                    ADC = new ADC(this, 4);
                    break;
                case Platform.UID3:
                case Platform.UID2:
                    ADC = new ADC(this, 2);
                    break;
            }
        }

        public override void Format(Action<int> progress = null)
        {
            base.Format(progress);
            //clear pictures, assume buffer is clear
            for (uint i = 0; i < Screen.FrameSize; i += Buffer.Length)
                Upload_Bmp((int)i, (int)Buffer.Length, false);
            for (var i = 0; i < Pictures.Length; i++)
                Pictures.TakeScreenshot(i);
        }

        public DeviceInfo GetDeviceInfo() => new DeviceInfo(this);
        public override string UploadExtensions => base.UploadExtensions + "JPG;BMP;WAV;";
        public override void Upload(Stream stream, string fileExt, int? index, bool verify = false)
        {
            switch (fileExt) { 
                case "JPG": 
                    Upload_Jpg(stream, true, (ushort)index.Value, verify);
                    break;
                case "BMP":
                    using (var bmp = new System.Drawing.Bitmap(stream))
                        Upload_Bmp(bmp.GetBytes(Screen.PixelFormat), true, verify);
                    Pictures.TakeScreenshot(index.Value);
                    break;
                case "WAV": 
                    Music.Upload(index.Value, stream, verify);
                    break;
                default:
                    base.Upload(stream, fileExt, index, verify);
                    break;
            }
        }

        //upload 16 bit per pixel data, 
        private void Upload_Bmp(byte[] data, bool swapBytes = false, bool verify = false)
        {
            var offset = 0;
            if (swapBytes) Extensions.SwapFileBytes(data, 0, data.Length);
            foreach (var v in new Slicer(new ArraySegment<byte>(data), Buffer.Length))
            {
                Buffer.Write(v, verify);
                Upload_Bmp(offset, v.Count, verify);
                offset += v.Count;
            }
        }

        //supported only on T5UID1, T5UID3
        private void Upload_Bmp(int imagePos, int length, bool verify = false)
        {
            var sramAddress = (Buffer.Address >> 1).ToLittleEndien(2);
            var dataLength = (length >> 1).ToLittleEndien(2);
            var imagePosition = (imagePos >> 1).ToLittleEndien(3);
            VP.Write(0xA2, new byte[] {
                0x5A, //fixed
                sramAddress[0], sramAddress[1], //SRAM position
                dataLength[0], dataLength[1], //data length in words
                imagePosition[0], imagePosition[1], imagePosition[2] //image buffer position
            });
            VP.Wait(0xA2);
        }

        //supported only on T5 devices
        private void Upload_Jpg(Stream stream, bool modeSave, ushort modeParam, bool verify = false)
        {
            RAM.Write(Buffer.Address, stream, verify);
            var sramAddress = (Buffer.Address >> 1).ToLittleEndien(2);
            var picIdOrPos = ((int)modeParam).ToLittleEndien(2);
            VP.Write(0xA6, new byte[] {
                0x5A, //fixed
                (byte)(modeSave ? 0x02 : 0x01), //display
                sramAddress[0], sramAddress[1], //sram position
                picIdOrPos[0], picIdOrPos[1], //screen position or pictureId
                0, 0 //undocumented, set 0
            });
            VP.Wait(0xA6);
        }

    }
}

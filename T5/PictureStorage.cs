using System;

namespace DgusDude.T5
{
    using Core;
    public class PictureStorage : Core.PictureStorage //, Core.IDrawPicture
    {
        public PictureStorage(Device device, uint length) 
            : base(device, length) { }

        public override int Current
        {
            get { return (int)Device.VP.Read(0x14, 2).FromLittleEndien(0, 2); }
            set
            {
                if (value > Length) throw Exception.CreateOutOfRange(value, Length);
                var picIdBytes = value.ToLittleEndien(2);
                Device.VP.Write(0x84, new byte[] { 0x5A, 0x01, picIdBytes[0], picIdBytes[1] });
                Device.VP.Wait(0x84);
            }
        }

        public override void TakeScreenshot(int pictureId)
        {
            if (pictureId > Length) throw Exception.CreateOutOfRange(pictureId, Length);
            var picIdBytes = pictureId.ToLittleEndien(2);
            Device.VP.Write(0xA6, new byte[] {
                    0x5A, //fixed
                    0x02, //save picture
                    picIdBytes[0], picIdBytes[1]
                });
        }

    }
}

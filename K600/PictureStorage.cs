using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DgusDude.K600
{
    using Core;
    public class PictureStorage : Core.PictureStorage //, Core.IDrawPicture
    {
        public PictureStorage(Device device, uint length)
            : base(device, length) { }

        public override int Current
        {
            get { return (int)Device.VP.Read(0x03, 2).FromLittleEndien(0, 2); }
            set
            {
                if (value > Length) throw Exception.CreateOutOfRange(value, Length);
                var picIdBytes = value.ToLittleEndien(2);
                Device.VP.Write(0x03, picIdBytes);
            }
        }
    }
}

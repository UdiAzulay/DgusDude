using System;

namespace DgusDude.Core
{
    public class LCD
    {
        public readonly uint Width;
        public readonly uint Height;
        public readonly decimal DiagonalInch;
        public readonly System.Drawing.Imaging.PixelFormat PixelFormat;
        public uint FrameSize => Width * Height * (uint)(System.Drawing.Image.GetPixelFormatSize(PixelFormat) / 8);
        public override string ToString() {
            return string.Format("{0} Inch, {1}x{2} {3}bpp", DiagonalInch, Width, Height, System.Drawing.Image.GetPixelFormatSize(PixelFormat));
        }

        public LCD(uint width, uint height, System.Drawing.Imaging.PixelFormat pixelFormat, decimal diagonalInch)
        {
            Width = width;
            Height = height;
            DiagonalInch = diagonalInch;
            PixelFormat = pixelFormat;
        }
    }
}

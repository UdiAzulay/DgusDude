using System;

namespace DgusDude.Core
{
    public static class Extensions
    {
#if NET5_0
        public static ArraySegment<byte> EmptyArraySegment = ArraySegment<byte>.Empty;
#else
        public static ArraySegment<byte> EmptyArraySegment = new ArraySegment<byte>(new byte[0]);
#endif
        public static byte[] ToLittleEndien(this int value, byte[] data, int startIndex, int length)
        {
            for (var i = startIndex + length - 1; i >= startIndex; i--, value >>= 8)
                data[i] = (byte)(value & 0xFF);
            return data;
        }
        public static byte[] ToLittleEndien(this int value, int length) =>
            ToLittleEndien(value, new byte[length], 0, length);
        public static long FromLittleEndien(this byte[] data, int startIndex, int length)
        {
            long ret = 0;
            for (var i = startIndex; i < startIndex + length; i++)
                ret = (ret << 8) | data[i];
            return ret;
        }
        public static int FromBigEndien(this byte[] data, int startIndex, int length)
        {
            int ret = 0;
            for (var i = startIndex + length; i >= startIndex; i--)
                ret = (ret << 8) | data[i];
            return ret;
        }

        public static int FromBCD(this byte data) { return (data & 0x0F) + ((data >> 4) * 10); }
        public static byte ToBCD(this int data) { return (byte)(((data / 10) << 4) + (data % 10)); }

        public static uint EnsureEvenLength(this uint length) { return length + (length % 2 != 0 ? 1u : 0u); }
        public static bool CompareBuffers(byte[] b1, int b1Offset, byte[] b2, int b2Offset, int length)
        {
            for (var i = 0; i < length; i++)
                if (b1[b1Offset + i] != b2[b2Offset + i]) return false;
            return true;
        }
        public static byte[] GetBytes(this System.Drawing.Bitmap bitmap, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            byte[] data;
            var bmpData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, pixelFormat);
            try
            {
                data = new byte[(bmpData.Width * bmpData.Height) * 2];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, data, 0, data.Length);
            }
            finally { bitmap.UnlockBits(bmpData); }
            return data;
        }


    }
}

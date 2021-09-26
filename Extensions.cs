using System;

namespace DgusDude
{
    public static class Extensions
    {
#if NETFRAMEWORK
        public static ArraySegment<byte> EmptyArraySegment = new ArraySegment<byte>(new byte[0]);
#else
        public static ArraySegment<byte> EmptyArraySegment = ArraySegment<byte>.Empty;
#endif
        public static byte[] ToLittleEndien(this uint value, byte[] data, int startIndex, int length)
        {
            for (var i = startIndex + length - 1; i >= startIndex; i--, value >>= 8)
                data[i] = (byte)(value & 0xFF);
            return data;
        }
        public static byte[] ToLittleEndien(this uint value, int length) =>
            ToLittleEndien(value, new byte[length], 0, length);
        public static ulong FromLittleEndien(this byte[] data, int startIndex, int length)
        {
            ulong ret = 0;
            for (var i = startIndex; i < startIndex + length; i++)
                ret = (ret << 8) | data[i];
            return ret;
        }

        public static ulong FromBigEndien(this byte[] data, int startIndex, int length)
        {
            ulong ret = 0;
            for (var i = startIndex + length; i >= startIndex; i--)
                ret = (ret << 8) | data[i];
            return ret;
        }

        public static uint EnsureEvenLength(this uint length) { return length + (length % 2 != 0 ? 1u : 0u); }

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

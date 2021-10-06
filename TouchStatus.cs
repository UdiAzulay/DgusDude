using System;

namespace DgusDude
{
    using Core;

    public enum TouchAction : byte { Release = 0, Press = 1, Lift = 2, Pressing = 3 }
    public class TouchStatus
    {
        public readonly TouchAction Status;
        public readonly uint X;
        public readonly uint Y;
        public override string ToString() => string.Format("X:{0} Y:{1} {2}", X, Y, Status);
        
        public TouchStatus(TouchAction status, uint x, uint y)
        {
            Status = status; X = x; Y = y;
        }

        public TouchStatus(byte[] data)
        {
            Status = (TouchAction)data[1];
            X = (uint)data.FromLittleEndien(2, 2);
            Y = (uint)data.FromLittleEndien(4, 2);
        }
    }
}

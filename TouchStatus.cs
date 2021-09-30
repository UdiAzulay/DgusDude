using System;

namespace DgusDude
{
    public enum TouchAction : byte { Release = 0, Press = 1, Lift = 2, Pressing = 3 }
    public class TouchStatus : Core.PackedData
    {
        public TouchStatus(Device device, int address, int readlen, bool refresh = true) : base(device.VP, address, new byte[readlen], refresh, false) { }
        public TouchAction Status => (TouchAction)Data[1];
        public uint X => (uint)Data.FromLittleEndien(2, 2);
        public uint Y => (uint)Data.FromLittleEndien(4, 2);
        public override string ToString() {
            return string.Format("X:{0} Y:{1} {2}", X, Y, Status);
        }
        protected override void Update(int offset, int length) { throw new DWINException("update of touch is not supported, use simulate instead"); }
        public virtual void Simulate(TouchAction action, uint x, uint y) { throw new NotImplementedException(); }
    }
}

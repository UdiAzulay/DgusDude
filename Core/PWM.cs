using System;
namespace DgusDude.Core
{
    public abstract class PWM
    {
        protected readonly Device Device;
        public int Length { get; private set; }
        public PWM(Device device, int length) { Device = device; Length = length; }
        public override string ToString() { return string.Format("{0} Items", Length); }
        public ushort this[int index] { get { return Read(index, 1)[0]; } set { } }
        protected void ValidateIndex(int index)
        {
            if (index > Length) throw new ArgumentOutOfRangeException("index");
        }

        public abstract void Write(byte index, byte div, ushort acuracy);
        public abstract ushort[] Read(int startIndex, int length);
    }
}

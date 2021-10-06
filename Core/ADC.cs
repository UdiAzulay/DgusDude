using System;
namespace DgusDude.Core
{
    public abstract class ADC
    {
        protected readonly Device Device;
        public int Length { get; private set; }
        public ADC(Device device, int length) { Device = device; Length = length; }
        public override string ToString() { return string.Format("{0} Items", Length); }
        public int this[int index] { get => Read(index, 1)[0]; }

        public abstract int[] Read(int startIndex, int length);
    }
}

using System;
using System.Linq;

namespace DgusDude.T5
{
    using Core;
    public class ADC
    {
        public readonly Device Device;
        public int Length { get; private set; }
        public ADC(Device device, int length = 7) { Device = device; Length = length; }
        public override string ToString() { return string.Format("{0} Items", Length); }
        public Tuple<ushort, ushort> this[int index] { get => Read(index, 1)[0]; }

        public Tuple<ushort, ushort>[] Read(int startIndex, int length)
        {
            var ret = Device.VP.Read(0x38 + (startIndex * 4), length * 4);
            return Enumerable.Range(0, ret.Length / 4)
                .Select(i => new Tuple<ushort, ushort>((ushort)ret.FromLittleEndien(i, 2), (ushort)ret.FromLittleEndien(i + 2, 2))).ToArray();
        }
    }
}

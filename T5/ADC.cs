using System;
using System.Linq;

namespace DgusDude.T5
{
    using Core;
    public class ADC : Core.ADC
    {
        public ADC(Device device, int length) : base(device, length) { }
        public override int[] Read(int startIndex, int length)
        {
            var ret = Device.VP.Read(0x32 + (startIndex * 2), length * 2);
            return Enumerable.Range(0, ret.Length / 2)
                .Select(i => (int)ret.FromLittleEndien(i, 2)).ToArray();
        }
    }
}

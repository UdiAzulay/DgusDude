using System;
using System.Linq;

namespace DgusDude.T5
{
    public class PWM
    {
        public readonly Device Device;
        public int Length { get; private set; }
        public PWM(Device device, int length = 3) { Device = device; Length = length; }
        public override string ToString() { return string.Format("{0} Items", Length); }
        public ushort this[int index] { get { return Read(index, 1)[0]; } set { } }
        public void SetPWM(byte index, byte div, UInt16 acuracy)
        {
            if (index > Length) throw new Exception("PWM indexes are 0-2");
            var accuracyBytes = ((int)acuracy).ToLittleEndien(2);
            Device.VP.Write(0x86 + (index * 4), new byte[] {
                    0x5A, // fixed
                    div, //division factor
                    accuracyBytes[0], accuracyBytes[1], //accuracy
                    0x00, 0x00, 0x00, 0x00 //fixed
                });
        }
        public ushort[] Read(int startIndex, int length)
        {
            if ((startIndex + length) > Length) throw new DWINException("PWM indexes are 0-" + Length);
            var ret = Device.VP.Read(0x92 + startIndex, 2 * length);
            return Enumerable.Range(0, length)
                .Select(v => (ushort)ret.FromLittleEndien(v * 2, 2)).ToArray();
        }
    }
}

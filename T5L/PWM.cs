using System;
using System.Linq;

namespace DgusDude.T5L
{
    using Core;
    public class PWM : T5.PWM
    {
        public PWM(Device device, int length) 
            : base(device, length) { }

        public override void Write(byte index, byte div, ushort acuracy)
        {
            ValidateIndex(index);
            var accuracyBytes = ((int)acuracy).ToLittleEndien(2);
            Device.VP.Write(0x86 + (index * 4), new byte[] {
                0x5A, // fixed
                div, //division factor
                accuracyBytes[0], accuracyBytes[1], //accuracy
            });
        }
    }
}

﻿using System;
using System.Linq;

namespace DgusDude.T5
{
    using Core;
    public class PWM : Core.PWM
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
                0x00, 0x00, 0x00, 0x00 //fixed
            });
        }

        public override ushort[] Read(int startIndex, int length)
        {
            ValidateIndex(startIndex);
            ValidateIndex(startIndex + length);
            var ret = Device.VP.Read(0x92 + startIndex, 2 * length);
            return Enumerable.Range(0, length)
                .Select(v => (ushort)ret.FromLittleEndien(v * 2, 2)).ToArray();
        }
    }
}

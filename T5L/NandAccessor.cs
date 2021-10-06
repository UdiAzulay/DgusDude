using System;

namespace DgusDude.T5L
{
    using Core;
    public class NandAccessor : MemoryBufferedAccessor
    {
        public NandAccessor(Device device, uint length, uint pageSize, uint blockSize) 
            : base(device, length, 4, pageSize, blockSize) {
        }

        public override void ValidateReadAddress(int address, uint length)
            => throw new NotSupportedException();

        protected override void WriteBlock(int address, int bufferAddress, uint length) {  }

        protected override void WritePage(int address, int bufferAddress, uint length)
        {
            var nandAddrBytes = ((int)(address / PageSize)).ToLittleEndien(2);
            var bufferAddrBytes = (bufferAddress >> 1).ToLittleEndien(2);
            Device.VP.Write(0xAA, new byte[] {
                0x5A, 0x02, //fixed, Write
                nandAddrBytes[0], nandAddrBytes[1], //font position
                bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                0x00, 0xFF, //how much nili to wait before next packet
                0x00, 0x00, 0x00, 0x00 //undefined should be 00
            });
            Device.VP.Wait(0xAA);
        }
    }
}

using System;

namespace DgusDude.T5
{
    using Core;
    public class NandAccessor : MemoryBufferedAccessor
    {
        public NandAccessor(Device device, uint length, uint pageSize, uint blockSize) 
            : base(device, length, 4, pageSize, blockSize) {
        }
        //can only read 16MB of the 64MB (1/4) in the space of 0x04:000000 - 0x7F:020000
        public override void ValidateReadAddress(int address, uint length)
        {
            base.ValidateReadAddress(address, length);
            byte bankStart = (byte)(address / PageSize);
            byte bankEnd = (byte)((address + length - 1) / PageSize);
            if (bankStart < 0x40 || bankStart > 0x7F || bankEnd < 0x40 || bankEnd > 0x7F)
                throw new System.Exception("Nand read area is only between 0x01000000:01FFFFFF (16MB of the 64MB)");
        }

        protected override void ValidateAlignment(bool isWrite, int address, uint length)
            => base.ValidateAlignment(isWrite, address, 0);
        private void NandWait() { Device.VP.Wait(0xAA); }

        //var banks = ((address + length) >> 18) - (address >> 18);
        protected override void ReadPage(int address, int bufferAddress, uint length)
        {
            var nandAddBytes = ((int)(address % PageSize) >> 1).ToLittleEndien(3);
            var bufferAddrBytes = (bufferAddress >> 1).ToLittleEndien(2);
            var lengthWords = ((int)(length >> 1)).ToLittleEndien(2); //Math.Max(bytesRead, 34) - minimum read block is (34 bytes)
            Device.VP.Write(0xAA, new byte[] {
                0x5A, 0x01, //fixed, Read
                (byte)(address / PageSize), //font ID 0x04 - 0x7F
                nandAddBytes[0], nandAddBytes[1], nandAddBytes[2], //font position
                bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                lengthWords[0], lengthWords[1], //read length
                0x00, 0x00 //undefined should be 0000
            });
            NandWait();
        }

        //can write on 32kb blocks on 32kb align address
        protected override void WriteBlock(int address, int bufferAddress, uint length) 
        {
            if ((Device.Platform & Platform.PlatformMask) == Platform.UID2)
                base.WriteBlock(address, bufferAddress, length);
            var bufferAddrBytes = (bufferAddress >> 1).ToLittleEndien(2);
            var nandAddrBytes = ((int)(address / BlockSize)).ToLittleEndien(2);
            Device.VP.Write(0xAA, new byte[] {
                0x5A, 0x02, //fixed, Write
                nandAddrBytes[0], nandAddrBytes[1], //font position
                bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                0x00, 0x01, //how much mili to wait before next packet, fixed at 01 for T5
                0x00, 0x00, 0x00, 0x00 //undefined should be 00
            });
            NandWait();
        }
        protected override void WritePage(int address, int bufferAddress, uint length)
        {
            if ((Device.Platform & Platform.PlatformMask) != Platform.UID2) {
                base.WritePage(address, bufferAddress, length);
                return;
            }
            var bufferAddrBytes = (bufferAddress >> 1).ToLittleEndien(2);
            var lengthBytes = ((int)length >> 1).ToLittleEndien(2);
            var nandAddrBytes = ((int)(address % PageSize) >> 1).ToLittleEndien(3);
            Device.VP.Write(0xAA, new byte[] {
                0x5A, 0x02, //fixed, Write
                (byte)(address / PageSize), //fontID
                nandAddrBytes[0], nandAddrBytes[1], nandAddrBytes[2], //font position
                bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                lengthBytes[0], lengthBytes[1], //length
                0x00, 0x00 //undefined should be 00
            });
            NandWait();
        }

    }
}

using System;

namespace DgusDude.T5
{
    public class NandAccessor : Core.MemoryBufferedAccessor
    {
        public NandAccessor(T5Device device, uint length, uint blockLength, uint pageSize = 0) 
            : base(device, length, 4, blockLength, pageSize) {
        }
        //can only read 16MB of the 64MB (1/4) in the space of 0x04:000000 - 0x7F:020000
        public override void ValidateReadAddress(int address, uint length)
        {
            base.ValidateReadAddress(address, length);
            byte bankStart = (byte)(address >> 18);
            byte bankEnd = (byte)(address + length - 1 >> 18);
            if (bankStart < 0x40 || bankStart > 0x7F || bankEnd < 0x40 || bankEnd > 0x7F)
                throw new Exception("Nand read area is only between 0x01000000:01FFFFFF (16MB of the 64MB)");
        }
        private void NandWait() { Device.VP.Wait(0xAA); }

        //var banks = ((address + length) >> 18) - (address >> 18);
        protected override void ReadBlock(int address, int bufferAddress, uint length)
        {
            var nandAddBytes = ((int)(address & BlockSize - 1) >> 1).ToLittleEndien(3);
            var bufferAddrBytes = (bufferAddress >> 1).ToLittleEndien(2);
            var lengthWords = ((int)(length >> 1)).ToLittleEndien(2); //Math.Max(bytesRead, 34) - minimum read block is (34 bytes)
            Device.VP.Write(0xAA, new byte[] {
                            0x5A, 0x01, //fixed, Read
                            (byte)(address >> 18), //font ID 0x04 - 0x7F
                            nandAddBytes[0], nandAddBytes[1], nandAddBytes[2], //font position
                            bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                            lengthWords[0], lengthWords[1], //read length
                            0x00, 0x00 //undefined should be 0000
                        });
            NandWait();
        }

        protected override void WriteBlock(int address, int bufferAddress, uint length) {  }
        //can write on 32kb blocks on 32kb align address
        protected override void WritePage(int address, int bufferAddress, uint length)
        {
            base.WritePage(address, bufferAddress, length);
            var nandAddrBytes = ((int)(address / PageSize)).ToLittleEndien(2);
            var bufferAddrBytes = (bufferAddress >> 1).ToLittleEndien(2);
            byte[] data;
            if ((Device.Platform & Platform.PlatformMask) != Platform.UID2) {
                data = new byte[] {
                    0x5A, 0x02, //fixed, Write
                    nandAddrBytes[0], nandAddrBytes[1], //font position
                    bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                    0x00, 0x01, //fixed
                    0x00, 0x00, 0x00, 0x00 //undefined should be 00
                };
            } else {
                //need to check
                data = new byte[] { 
                    0x5A, 0x02, //fixed, Write
                    nandAddrBytes[0], nandAddrBytes[1], //font position
                    bufferAddrBytes[0], bufferAddrBytes[1], //SRAM address
                    0x00, 0x01, //fixed
                    0x00, 0x00, 0x00, 0x00 //undefined should be 00
                };
            }
            Device.VP.Write(0xAA, data);
            NandWait();
        }
    }
}

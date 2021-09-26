using System;

namespace DgusDude.Core.T5
{
    public class NandAccessor : MemoryBufferedAccessor
    {
        public const uint FONT_BLOCK_SIZE = 0x040000; //256Kb
        public const uint NAND_WRITE_MIN_LEN = 0x8000; //32kb
        //public readonly uint BlockSize;

        public NandAccessor(T5Base device, uint length) : base(device, length, 4, (int)NAND_WRITE_MIN_LEN) { }
        //can only read 16MB of the 64MB (1/4) in the space of 0x04:000000 - 0x7F:020000
        public override void ValidateReadAddress(uint address, int length)
        {
            base.ValidateReadAddress(address, length);
            byte bankStart = (byte)(address >> 18);
            byte bankEnd = (byte)((address + length - 1) >> 18);
            if (bankStart < 0x40 || bankStart > 0x7F || bankEnd < 0x40 || bankEnd > 0x7F)
                throw new Exception("Nand read area is only between 0x01000000:01FFFFFF (16MB of the 64MB)");
        }

        public override void ValidateWriteAddress(uint address, int length)
        {
            base.ValidateWriteAddress(address, length);
            if ((length % NAND_WRITE_MIN_LEN) != 0 || (address % NAND_WRITE_MIN_LEN) != 0)
                throw new Exception("Nand can only write the 64MB font memory in 32KB blocks on 32KB align address");
        }

        private void NandWait() { Device.VP.Wait(0xAA); }

        public override void Read(uint address, uint targetAddress, int length)
        {
            ValidateReadAddress(address, length);
            Device.SRAM.ValidateWriteAddress(targetAddress, length);
            //uint bytesRead = 0;
            //uint endAddress = address + (uint)length;
            //var banks = ((address + length) >> 18) - (address >> 18);

            using (var slices = new Slicer((uint)length, FONT_BLOCK_SIZE).GetEnumerator())
            {
                while (slices.MoveNext())
                {
                    var nandAddress = ((address & (FONT_BLOCK_SIZE - 1)) >> 1).ToLittleEndien(3);
                    var bufferAddress = (targetAddress >> 1).ToLittleEndien(2);
                    var lengthWords = (slices.CurrentLength >> 1).ToLittleEndien(2); //Math.Max(bytesRead, 34) - minimum read block is (34 bytes)
                    Device.VP.Write(0xAA, new byte[] {
                            0x5A, 0x01, //fixed, Read
                            (byte)(address >> 18), //font ID 0x04 - 0x7F
                            nandAddress[0], nandAddress[1], nandAddress[2], //font position
                            bufferAddress[0], bufferAddress[1], //SRAM address
                            lengthWords[0], lengthWords[1], //read length
                            0x00, 0x00 //undefined should be 0000
                        });
                    NandWait();
                    address += (uint)slices.CurrentLength;
                    targetAddress += (uint)slices.CurrentLength;
                }
            }
        }

        //can write on 32kb blocks on 32kb align address
        public override void Write(uint address, uint srcAddress, int length)
        {
            var padBytes = length % (int)NAND_WRITE_MIN_LEN;
            if (padBytes != 0) padBytes = (int)NAND_WRITE_MIN_LEN - padBytes;
            ValidateWriteAddress(address, length + padBytes);
            Device.SRAM.ValidateReadAddress(srcAddress, length + padBytes);
            var nandAddress = (address / NAND_WRITE_MIN_LEN).ToLittleEndien(2);
            var bufferAddress = (srcAddress >> 1).ToLittleEndien(2);
            Device.VP.Write(0xAA, new byte[] {
                    0x5A, 0x02, //fixed, Write
                    nandAddress[0], nandAddress[1], //font position
                    bufferAddress[0], bufferAddress[1], //SRAM address
                    0x00, 0x01, //fixed
                    0x00, 0x00, 0x00, 0x00 //undefined should be 00
                });
            NandWait();
        }

    }
}

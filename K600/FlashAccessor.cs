using System;

namespace DgusDude.K600
{
    using Core;
    public class FlashAccessor : MemoryBufferedAccessor
    {
        public FlashAccessor(Device device, uint length, byte alignment, uint pageSize) : base(device, length, alignment, pageSize, 0) { }
        private void SendIO(bool write, int address, int bufferAddress, uint length)
        {
            var lengthWords = ((int)(length >> 1).EnsureEvenLength()).ToLittleEndien(2);
            var ramAddress = (bufferAddress >> 1).ToLittleEndien(2);
            var bankId = (byte)(address / PageSize);
            var nandAddress = ((int)(address % PageSize) >> 1).ToLittleEndien(3);
            Device.VP.Write(0x30, new byte[] {
                0x5A, //fixed 
                (byte)(write ? 0x50 : 0xA0), //read/write
                bankId, //each bank is 128kw
                nandAddress[0], nandAddress[1], nandAddress[2],
                ramAddress[0], ramAddress[1], //SRAM address
                lengthWords[0], lengthWords[1] //length
            });
            Device.VP.Wait(0x30);
        }

        protected override void ReadPage(int address, int bufferAddress, uint length)
        {
            SendIO(false, address, bufferAddress, length);
        }

        protected override void WritePage(int address, int bufferAddress, uint length)
        {
            SendIO(true, address, bufferAddress, length);
        }
    }
}

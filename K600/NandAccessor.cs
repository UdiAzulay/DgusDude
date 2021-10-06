using System;

namespace DgusDude.K600
{
    using Core;
    public class NandAccessor : MemoryBufferedAccessor
    {
        public NandAccessor(Device device, uint length, byte alignment, uint pageSize) : base(device, length, alignment, pageSize, 0) { }
        private void SendIO(bool write, int address, int bufferAddress, uint length)
        {
            var lengthWords = ((int)(length >> 1).EnsureEvenLength()).ToLittleEndien(2);
            var ramAddress = (bufferAddress >> 1).ToLittleEndien(2);
            var nandAddress = (address >> 1).ToLittleEndien(4);
            var cmd = write ? (byte)0x50 : (byte)0xA0;
            Device.VP.Write(0x56, new byte[] {
                    0x5A, cmd, //fixed - read/write
                    nandAddress[0], nandAddress[1], nandAddress[2], nandAddress[3],
                    ramAddress[0], ramAddress[1], //SRAM address
                    lengthWords[0], lengthWords[1] //length
                });
            Device.VP.Wait(0x56);
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

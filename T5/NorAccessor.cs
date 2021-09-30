using System;

namespace DgusDude.T5
{
    using Core;
    public class NorAccessor : Core.MemoryBufferedAccessor
    {
        public NorAccessor(T5Device device, uint length) : base(device, length, 4, 0x1000 /*4k*/) { }
        private void SendIO(bool write, int address, int bufferAddress, uint length)
        {
            var lengthWords = ((int)(length >> 1).EnsureEvenLength()).ToLittleEndien(2);
            var ramAddress = (bufferAddress >> 1).ToLittleEndien(2);
            var norAddress = (address >> 1).ToLittleEndien(3);
            var cmd = write ? (byte)0xA5 : (byte)0x5A;
            Device.VP.Write(0x08, new byte[] {
                    cmd, //fixed - read/write
                    norAddress[0], norAddress[1], norAddress[2], //nor address
                    ramAddress[0], ramAddress[1], //SRAM address
                    lengthWords[0], lengthWords[1] //length
                });
            Device.VP.Wait(0x08, cmd);
        }

        //if ((address & 0xFFFFFF) > 0x1FFFF) throw new Exception("Nor block is only 0x1FFFF bytes");
        protected override void ReadBlock(int address, int bufferAddress, uint length)
        {
            SendIO(false, address, bufferAddress, length);
        }
        protected override void WriteBlock(int address, int bufferAddress, uint length)
        {
            SendIO(true, address, bufferAddress, length);
        }

    }
}

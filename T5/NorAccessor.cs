using System;

namespace DgusDude.Core.T5
{
    public class NorAccessor : MemoryBufferedAccessor
    {
        public NorAccessor(T5Base device, uint length) : base(device, length, 4, 0x1000 /*4k*/) { }

        private void SendIO(bool write, uint address, uint bufferAddress, int length)
        {
            var lengthWords = ((uint)length >> 1).EnsureEvenLength().ToLittleEndien(2);
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

        public override void Read(uint address, uint targetAddress, int length)
        {
            //if ((address & 0xFFFFFF) > 0x1FFFF) throw new Exception("Nor block is only 0x1FFFF bytes");
            ValidateReadAddress(address, length);
            Device.SRAM.ValidateWriteAddress(targetAddress, length);
            SendIO(false, address, targetAddress, length);
        }
        public override void Write(uint address, uint srcAddress, int length)
        {
            ValidateWriteAddress(address, length);
            Device.SRAM.ValidateReadAddress(srcAddress, length);
            SendIO(true, address, srcAddress, length);
        }
    }
}

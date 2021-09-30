using System;

namespace DgusDude.Core
{
    public class VP : IDeviceAccessor
    {
        private const uint VP_WAIT_TIMEOUT = 1500;
        private byte _addressShift = 0;
        public readonly MemoryAccessor Memory;
        public VP(MemoryAccessor mem) { Memory = mem; _addressShift = (byte)(mem.Alignment >> 1); }
        public override string ToString() {
            return string.Format("VP On {0}", Memory == Memory.Device.Registers ? "Registers" : "SRAM");
        }
        public virtual void Read(int address, ArraySegment<byte> data) { Memory.Read(address << _addressShift, data); }
        public virtual void Write(int address, ArraySegment<byte> data) { Memory.Write(address << _addressShift, data); }
        void IDeviceAccessor.Write(int address, ArraySegment<byte> data, bool verify) { Write(address, data); }
        public void Write(int address, byte[] data) { Write(address, new ArraySegment<byte>(data)); }
        public byte[] Read(int address, int len)
        {
            var ret = new byte[len];
            Read(address, new ArraySegment<byte>(ret));
            return ret;
        }

        public virtual void Wait(int address, int checkValue = 0x5A, int checkIndex = 0, int readlength = 2)
        {
            var loopCount = VP_WAIT_TIMEOUT / 100;
            while (Read(address, readlength)[checkIndex] == checkValue && loopCount > 0)
            {
                System.Threading.Thread.Sleep(100);
                loopCount--;
            }
            if (loopCount <= 0)
                throw new Exception(string.Format("DeviceWait indication is not clear at VP 0x{0:X4}", address));
        }
    }
}

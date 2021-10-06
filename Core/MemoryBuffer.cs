using System;

namespace DgusDude.Core
{
    public class MemoryBuffer
    {
        public readonly MemoryAccessor Memory;
        public int Address { get; set; }
        public uint Length { get; set; }
        public MemoryBuffer(MemoryAccessor memory, int address, uint length)
        {
            Memory = memory; Address = address; Length = length;
        }

        public override string ToString()
        {
            return string.Format("{0}kb, 0x{1:X}:{2:X}", Length / 1024, Address, Address + Length);
        }

        public void Write(ArraySegment<byte> data, bool verify = false, int address = 0)
        {
            if (data.Count > Length) throw new System.Exception("buffer size is too small, use Buffer.Length to enlarge it");
            Memory.Write(Address + address, data, verify);
        }

        public void Read(ArraySegment<byte> data, int address = 0)
        {
            Memory.Read(Address + address, data);
        }

        public void Clear(uint length = uint.MaxValue, bool verify = false)
        {
            if (length > Length) length = Length;
            Memory.MemSet(Address, length, Extensions.EmptyArraySegment, verify);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DgusDude.Core
{
    public abstract class MemoryBufferedAccessor : MemoryAccessor
    {
        protected MemoryBufferedAccessor(Device device, uint length, byte alignment, uint maxPacketLength) : base(device, length, alignment, maxPacketLength) { }
        public abstract void Read(uint address, uint bufferAddress, int length);
        public abstract void Write(uint address, uint bufferAddress, int length);

        public override void Read(uint address, ArraySegment<byte> data)
        {
            foreach (var v in new Slicer(data, MaxPacketLength))
            {
                Read(address, Device.BufferAddress, v.Count);
                Device.SRAM.Read(Device.BufferAddress, v);
                address += (uint)v.Count;
            }
        }

        public override void Verify(uint address, ArraySegment<byte> data)
        {
            foreach (var v in new Slicer(data, MaxPacketLength))
            {
                Read(address + (uint)v.Offset, Device.BufferAddress + Alignment, v.Count);
                Device.SRAM.Verify(Device.BufferAddress + Alignment, v);
            }
        }

        public override void Write(uint address, ArraySegment<byte> data, bool verify = false)
        {
            foreach (var v in new Slicer(data, MaxPacketLength))
            {
                Device.SRAM.Write(Device.BufferAddress, v, verify);
                Write(address, Device.BufferAddress, v.Count);
                if (verify) Verify(address, data);
                address += (uint)v.Count;
            }
        }

        public override void MemSet(uint address, int length, ArraySegment<byte> data, bool verify = false)
        {
            var offset = 0;
            data = CreatePatternBuffer(data, MaxPacketLength);
            Device.SRAM.Write(Device.BufferAddress, data, verify);
            while (offset < length)
            {
                Write(address + (uint)offset, Device.BufferAddress, data.Count);
                offset += data.Count;
            }
        }

    }
}

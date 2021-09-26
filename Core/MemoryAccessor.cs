using System;
using System.Collections.Generic;
using System.Text;

namespace DgusDude.Core
{
    public interface IDeviceAccessor
    {
        //readonly Core.Device Device;
        void Read(uint address, ArraySegment<byte> data);
        void Write(uint address, ArraySegment<byte> data, bool verify = false);
    }

    public abstract class MemoryAccessor : IDeviceAccessor
    {
        protected static byte[] WritePaddingByte = new byte[] { 0x00 };
        public readonly Device Device;
        public uint Length { get; private set; }
        public byte Alignment { get; private set; }
        public uint MaxPacketLength { get; private set; }
        protected MemoryAccessor(Device device, uint length, byte alignment, uint maxPacketLength) { Device = device; Length = length; Alignment = alignment; MaxPacketLength = maxPacketLength; }
        protected virtual void ValidateAddress(uint address, int length)
        {
            if (address >= Length) throw DWINException.CreateOutOfRange(this, address);
            if (address + (uint)length > Length) throw DWINException.CreateOutOfRange(this, address + (uint)length - 1);
            if ((address % Alignment != 0) || (length % Alignment != 0)) throw DWINException.CreateMemBaundary(this, Alignment);
        }
        public virtual void ValidateReadAddress(uint address, int length) { ValidateAddress(address, length); }
        public virtual void ValidateWriteAddress(uint address, int length) { ValidateAddress(address, length); }
        public abstract void Read(uint address, ArraySegment<byte> data);
        public abstract void Write(uint address, ArraySegment<byte> data, bool verify = false);
        public virtual void Verify(uint address, ArraySegment<byte> data)
        {
            byte[] readBuffer = new byte[data.Count + (data.Count % 2 == 1 ? 1 : 0)];
            Read(address, new ArraySegment<byte>(readBuffer));
            for (int i = 0; i < data.Count; i++)
                if (readBuffer[i] != data.Array[data.Offset + i])
                    throw new DWINVerifyException(string.Format("Write verify exception at {0:X}", address + i));
        }

        protected ArraySegment<byte> CreatePatternBuffer(ArraySegment<byte> data, uint bufMinSize)
        {
            if (bufMinSize < data.Count * 2) return data;
            var patCount = (bufMinSize / data.Count);
            var newData = new byte[patCount * data.Count];
            for (var i = 0; i < patCount; i++)
                Array.Copy(data.Array, data.Offset, newData, i * data.Count, data.Count);
            return new ArraySegment<byte>(newData);
        }

        public virtual void MemSet(uint address, int length, ArraySegment<byte> data, bool verify = false)
        {
            var offset = 0;
            data = CreatePatternBuffer(data, MaxPacketLength);
            while (offset < length)
            {
                var dt = new ArraySegment<byte>(data.Array,
                    data.Offset + (offset % data.Count),
                    Math.Min(data.Count, length - offset));
                Write(address + (uint)offset, dt, verify);
                offset += dt.Count;
            }
        }
    }
}

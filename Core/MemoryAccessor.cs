using System;
namespace DgusDude.Core
{
    public interface IDeviceAccessor
    {
        void Read(int address, ArraySegment<byte> data);
        void Write(int address, ArraySegment<byte> data, bool verify = false);
    }

    public abstract class MemoryAccessor : IDeviceAccessor
    {
        public readonly Device Device;
        public uint Length { get; private set; }
        public uint BlockSize { get; private set; }
        public uint PageSize { get; private set; }
        public byte Alignment { get; private set; }
        protected uint OptimalIOLength => (BlockSize != 0 ? BlockSize : Device.Buffer.Length);
        protected MemoryAccessor(Device device, uint length, byte alignment = 1, uint pageSize = 0, uint blockSize = 0) 
        { 
            Device = device; 
            Length = length; 
            Alignment = alignment;
            BlockSize = blockSize;
            PageSize = pageSize;
        }

        public override string ToString()  
           => string.Format("{0:#,#\\k\\b}\t(Align:{1}, Page:{2}, Block:{3})", Length >> 10, Alignment, PageSize, BlockSize); 

        protected virtual void ValidateAddress(bool isWrite, int address, uint length)
        {
            if (address >= Length) throw Exception.CreateOutOfRange(this, address);
            if (address + (uint)length > Length) throw Exception.CreateOutOfRange(this, (int)(address + length - 1));
            ValidateAlignment(isWrite, address, length);
        }
        protected virtual void ValidateAlignment(bool isWrite, int address, uint length)
        {
            if (address % Alignment != 0 || length % Alignment != 0) throw Exception.CreateMemBaundary(this, Alignment);
        }

        public virtual void ValidateReadAddress(int address, uint length) { ValidateAddress(false, address, length); }
        protected virtual void ReadBlock(int address, ArraySegment<byte> data) { throw new NotImplementedException(); }
        protected virtual void ReadPage(int address, ArraySegment<byte> data) 
        {
            foreach (var block in new Slicer(data, BlockSize))
            {
                ReadBlock(address, block);
                address += block.Count;
            }
        }
        public virtual void Read(int address, ArraySegment<byte> data)
        {
            ValidateReadAddress(address, (uint)data.Count);
            foreach (var page in new Slicer(data, PageSize, address))
            {
                ReadPage(address, page);
                address += page.Count;
            }
        }

        public virtual void ValidateWriteAddress(int address, uint length) { ValidateAddress(true, address, length); }
        protected virtual void WriteBlock(int address, ArraySegment<byte> data, bool verify = false) { throw new NotImplementedException(); }
        protected virtual void WritePage(int address, ArraySegment<byte> data, bool verify = false) 
        {
            foreach (var block in new Slicer(data, BlockSize))
            {
                WriteBlock(address, block, verify);
                address += block.Count;
            }
        }
        public virtual void Write(int address, ArraySegment<byte> data, bool verify = false)
        {
            ValidateWriteAddress(address, (uint)data.Count);
            foreach (var page in new Slicer(data, PageSize, address))
            {
                WritePage(address, page, verify);
                address += page.Count;
            }
        }

        public virtual void Write(int address, System.IO.Stream stream, bool verify = false)
        {
            ValidateWriteAddress(address, (uint)(stream.Length - stream.Position));
            int bytesRead;
            var buffer = new byte[OptimalIOLength];
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) { 
                Write(address, new ArraySegment<byte>(buffer, 0, bytesRead), verify);
                address += bytesRead;
            }
        }

        public virtual void Verify(int address, ArraySegment<byte> data)
        {
            byte[] readBuffer = new byte[data.Count + (data.Count % Alignment)];
            Read(address, new ArraySegment<byte>(readBuffer));
            for (int i = 0; i < data.Count; i++)
                if (readBuffer[i] != data.Array[data.Offset + i])
                    throw new VerifyException(string.Format("Write verify exception at {0:X}", address + i));
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

        public virtual void MemSet(int address, uint length, ArraySegment<byte> data, bool verify = false)
        {
            var offset = 0;
            var blockSize = OptimalIOLength;
            if (data != Extensions.EmptyArraySegment) data = CreatePatternBuffer(data, blockSize);
            else data = new ArraySegment<byte>(new byte[blockSize]);
            while (offset < length)
            {
                var dt = new ArraySegment<byte>(data.Array,
                    data.Offset + (offset % data.Count),
                    Math.Min(data.Count, (int)(length - offset)));
                Write(address + offset, dt, verify);
                offset += dt.Count;
            }
        }
    }
}

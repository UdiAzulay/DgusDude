using System;

namespace DgusDude.Core
{
    public abstract class MemoryBufferedAccessor : MemoryAccessor
    {
        protected virtual MemoryBuffer Buffer => Device.Buffer;
        protected MemoryBufferedAccessor(Device device, uint length, byte alignment, uint pageSize, uint blockSize) : base(device, length, alignment, pageSize, blockSize) { }

        protected virtual void ReadBlock(int address, int bufferAddress, uint length)
        {
            throw new NotImplementedException();
        }
        protected virtual void ReadPage(int address, int bufferAddress, uint length)
        {
            using (var blocks = new Slicer((uint)length, BlockSize).GetEnumerator())
            {
                while (blocks.MoveNext()) {
                    var readLength = (int)blocks.CurrentLength;
                    ReadBlock(address, bufferAddress, (uint)readLength);
                    address += readLength;
                    bufferAddress += readLength;
                }
            }
        }
        public virtual void Read(int address, int bufferAddress, uint length)
        {
            ValidateReadAddress(address, length);
            Device.RAM.ValidateWriteAddress(bufferAddress, length);
            using (var pages = new Slicer((uint)length, PageSize).GetEnumerator())
            {
                while (pages.MoveNext())
                {
                    var readLength = (int)pages.CurrentLength;
                    ReadPage(address, bufferAddress, (uint)readLength);
                    address += readLength;
                    bufferAddress += readLength;
                }
            }
        }

        protected virtual void WriteBlock(int address, int bufferAddress, uint length)
        {
            throw new NotImplementedException();
        }
        protected virtual void WritePage(int address, int bufferAddress, uint length)
        {
            using (var blocks = new Slicer((uint)length, BlockSize).GetEnumerator())
            {
                while (blocks.MoveNext()) {
                    var writeLength = (int)blocks.CurrentLength;
                    WriteBlock(address, bufferAddress, (uint)writeLength);
                    address += writeLength;
                    bufferAddress += writeLength;
                }
            }
        }
        public virtual void Write(int address, int bufferAddress, uint length)
        {
            ValidateWriteAddress(address, length);
            Device.RAM.ValidateReadAddress(bufferAddress, length);
            using (var pages = new Slicer((uint)length, PageSize).GetEnumerator())
            {
                while (pages.MoveNext())
                {
                    var readLength = (int)pages.CurrentLength;
                    WritePage(address, bufferAddress, (uint)readLength);
                    address += readLength;
                    bufferAddress += readLength;
                }
            }
        }

        public override void Read(int address, ArraySegment<byte> data)
        {
            foreach (var part in new Slicer(data, Buffer.Length)) { 
                Read(address, Buffer.Address, (uint)part.Count);
                Buffer.Read(part);
                address += part.Count;
            }
        }

        public override void Write(int address, ArraySegment<byte> data, bool verify = false)
        {
            foreach (var part in new Slicer(data, Buffer.Length))
            {
                Buffer.Write(part, verify);
                Write(address, Buffer.Address, (uint)part.Count);
                address += part.Count;
            }
        }

        public override void Verify(int address, ArraySegment<byte> data)
        {
            foreach (var part in new Slicer(data, Buffer.Length))
            {
                Read(address, Buffer.Address + Alignment, (uint)part.Count);
                Buffer.Memory.Verify(Buffer.Address + Alignment, part);
                address += part.Count;
            }
        }

        public override void MemSet(int address, uint length, ArraySegment<byte> data, bool verify = false)
        {
            var offset = 0;
            data = CreatePatternBuffer(data, Math.Min(OptimalIOLength, length));
            Buffer.Write(data, verify);
            while (offset < length)
            {
                Write(address + offset, Buffer.Address, (uint)Math.Min(data.Count, length - offset));
                offset += data.Count;
            }
        }
    }
}

using System;

namespace DgusDude.Core
{
    public abstract class MemoryBufferedAccessor : MemoryAccessor
    {
        protected virtual MemoryBuffer Buffer => Device.Buffer;
        protected MemoryBufferedAccessor(Device device, uint length, byte alignment, uint packetLength, uint pageSize = 0) : base(device, length, alignment, packetLength, pageSize) { }

        protected virtual void ReadBlock(int address, int bufferAddress, uint length)
        {
            throw new NotImplementedException();
        }
        protected virtual void ReadPage(int address, int bufferAddress, uint length)
        {
            using (var blocks = new Slicer((uint)length, PageSize).GetEnumerator())
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
            using (var blocks = new Slicer((uint)length, PageSize).GetEnumerator())
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

        protected override void ReadBlock(int address, ArraySegment<byte> data)
        {
            Read(address, Buffer.Address, (uint) data.Count);
            Buffer.Read(data);
        }

        protected override void WriteBlock(int address, ArraySegment<byte> data, bool verify = false)
        {
            Buffer.Write(data, verify);
            Write(address, Buffer.Address, (uint) data.Count);
            if (verify) Verify(address, data);
        }

        public override void Verify(int address, ArraySegment<byte> data)
        {
            foreach (var v in new Slicer(data, BlockSize))
            {
                Read(address + v.Offset, Buffer.Address + Alignment, (uint) v.Count);
                Buffer.Memory.Verify(Buffer.Address + Alignment, v);
            }
        }

        public override void MemSet(int address, uint length, ArraySegment<byte> data, bool verify = false)
        {
            var offset = 0;
            data = CreatePatternBuffer(data, BlockSize);
            Buffer.Write(data, verify);
            while (offset < length)
            {
                Write(address + offset, Buffer.Address, (uint) data.Count);
                offset += data.Count;
            }
        }

    }
}

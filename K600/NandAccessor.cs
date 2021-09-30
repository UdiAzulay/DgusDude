using System;

namespace DgusDude.K600
{
    public class NandAccessor : Core.MemoryBufferedAccessor
    {
        public NandAccessor(Device device, uint length, byte alignment, uint blockSize, uint pageSize = 0) : base(device, length, alignment, blockSize, pageSize) { }
        public override void Read(int address, int bufferAddress, uint length)
        {
            throw new NotImplementedException();
        }

        public override void Write(int address, int bufferAddress, uint length)
        {
            throw new NotImplementedException();
        }
    }
}

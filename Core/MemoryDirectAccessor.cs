using System;

namespace DgusDude.Core
{
    public class MemoryDirectAccessor : MemoryAccessor
    {
        private static byte[] PaddingBytes = new byte[4];
        public readonly byte ReadCommand, WriteCommand;
        public readonly AddressMode AddressMode;
        public MemoryDirectAccessor(Device device,
            uint length, byte alignment,
            AddressMode addressMode, byte readCommand, byte writeCommand,
            uint pageSize = 0, uint blockSize = Device.MAX_PACKET_SIZE
            )  : base(device, length, alignment, pageSize, blockSize) 
        {
            AddressMode = addressMode;
            ReadCommand = readCommand; WriteCommand= writeCommand;
        }
        protected override void ValidateAlignment(bool isWrite, int address, uint length)
            => base.ValidateAlignment(isWrite, address, 0);
        protected virtual PacketHeader GetReadHeader(int address, ArraySegment<byte> data, ArraySegment<byte> padding, out PacketHeader replyHeader)
        {
            replyHeader = new PacketHeader(AddressMode, Device.Config.Header.Length, 1);
            var ret = new PacketHeader(Device.Config.Header, AddressMode, 1) { 
                Command = ReadCommand, 
                Address = address, 
                DataLength = 1 
            };
            var readLength = (uint)(data.Count + padding.Count);
            if ((AddressMode & AddressMode.ShiftRead) != 0) readLength >>= ret.AddressShift;
            ret.Data.Array[ret.DataOffset] = (byte)readLength;
            return ret;
        }
        
        protected virtual void ReadValidateBlock(int address, PacketHeader sentHeader, PacketHeader replyHeader, ArraySegment<byte> data)
        {
            if (replyHeader.Validate(Device.Config.Header, ReadCommand)) return;
            throw VerifyException.CreateValidate(this, address, "Read");
        }

        protected override void ReadBlock(int address, ArraySegment<byte> data)
        {
            PacketHeader readHeader;
            var padding = new ArraySegment<byte>(PaddingBytes, 0, (byte)(data.Count % Alignment));
            var writeHeader = GetReadHeader(address, data, padding, out readHeader);
            var retries = Device.Config.Retries - 1;
            for (var r = 0; r <= retries; r++)
                try {
                    Device.RawWrite(r, writeHeader.Data);
                    Device.RawRead(r, readHeader.Data, data, padding);
                    ReadValidateBlock(address, writeHeader, readHeader, data);
                    break;
                } catch (TimeoutException) {
                    if (r == retries) throw Exception.CreateTimeout(writeHeader.Command, address);
                    System.Threading.Thread.Sleep(Device.Config.RetryWait);
                }
        }

        protected virtual PacketHeader GetWriteHeader(int address, ArraySegment<byte> data, ArraySegment<byte> padding, out PacketHeader replyHeader)
        {
            var devConfig = Device.Config;
            replyHeader = ((devConfig.Options & ConnectionOptions.NoAckRAM) == 0 || WriteCommand != 0x82) ? 
                new PacketHeader(0, devConfig.Header.Length, devConfig.AckValue.Length) : null;
            return new PacketHeader(devConfig.Header, AddressMode, 0)
            {
                Command = WriteCommand,
                Address = address,
                DataLength = (byte)(data.Count + padding.Count)
            };
        }

        protected virtual void WriteValidateBlock(int address, PacketHeader sentHeader, PacketHeader replyHeader, ArraySegment<byte> data) 
        {
            if (replyHeader.Validate(Device.Config.Header, WriteCommand, Device.Config.AckValue)) return;
            throw VerifyException.CreateValidate(this, address, "Write");
        }

        protected override void WriteBlock(int address, ArraySegment<byte> data, bool verify = false)
        {
            PacketHeader readHeader;
            var padding = new ArraySegment<byte>(PaddingBytes, 0, (byte)(data.Count % Alignment));
            var writeHeader = GetWriteHeader(address, data, padding, out readHeader);
            var retries = Device.Config.Retries - 1;
            for (var r = 0; r <= retries; r++)
                try {
                    Device.RawWrite(r, writeHeader.Data, data, padding);
                    if (readHeader != null) {
                        Device.RawRead(r, readHeader.Data);
                        WriteValidateBlock(address, writeHeader, readHeader, data);
                    }
                    if (verify) Verify(address, data);
                    break;
                } catch (VerifyException) {
                    if (r == retries) throw;
                } catch (TimeoutException) {
                    if (r == retries) throw Exception.CreateTimeout(writeHeader.Command, address);
                    System.Threading.Thread.Sleep(Device.Config.RetryWait);
                }
        }
    }

}

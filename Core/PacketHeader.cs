using System;

namespace DgusDude.Core
{
    [Flags]
    public enum AddressMode : byte
    {
        Size1 = 0x01, Size2 = 0x02, //Size3 = 0x03, 
        SizeMask = 0x03,
        Shift1 = 0x04, //Shift2 = 0x06, Shift3 = 0x0C,
        ShiftMask = 0x0C,
        ShiftRead = 0x10,
        Reserved = 0x20,
        Header1 = 0x40,
    }
    public class PacketHeader
    {
        public readonly AddressMode AddressMode;
        public readonly ArraySegment<byte> Data;
        private byte GetByte(int index) { return Data.Array[Data.Offset + index]; }
        private void SetByte(int index, byte value) { Data.Array[Data.Offset + index] = value; }
        public PacketHeader(AddressMode addressMode, ArraySegment<byte> data) {
            AddressMode = addressMode; Data = data; 
        }
        public PacketHeader(AddressMode addressMode, int headerLength, int extraData) 
            : this(
                  addressMode | (AddressMode)(headerLength * (byte)AddressMode.Header1), 
                  new ArraySegment<byte>(new byte[2 + headerLength + extraData +
                    ((byte)addressMode & (byte)AddressMode.SizeMask)]))
        {  }

        public PacketHeader(byte[] header, AddressMode addressMode, int extraData) 
            : this(addressMode, (header?.Length).GetValueOrDefault(), extraData) 
        {
            if (header != null)
                Array.Copy(header, 0, Data.Array, Data.Offset, header.Length);
        }
        private byte AddressLength => (byte)(AddressMode & AddressMode.SizeMask);
        public byte AddressShift => (byte)(((byte)AddressMode / (byte)AddressMode.Shift1) & (byte)AddressMode.SizeMask);
        private byte HeaderLength { get { return (byte)((byte)AddressMode / (byte)AddressMode.Header1); } }
        private byte FixedLength => (1 /*packet len*/ + 1 /*command*/);
        public byte DataOffset => (byte)(HeaderLength + FixedLength + AddressLength);
        public byte PacketLength { get { return GetByte(HeaderLength); } set { SetByte(HeaderLength, value); } }
        public byte Command { get { return GetByte(HeaderLength + 1); } set { SetByte(HeaderLength + 1, value); } }
        public byte DataLength { get { return (byte)(PacketLength - (AddressLength + 1)); } set { PacketLength = (byte)(value + (AddressLength + 1)); } }
        public int Address
        {
            set { (value >> AddressShift).ToLittleEndien(Data.Array, Data.Offset + HeaderLength + 2, AddressLength); }
            get { return (int) (Data.Array.FromLittleEndien(Data.Offset + HeaderLength + 2, AddressLength) << AddressShift); }
        }

        public bool Validate(byte[] header, byte? command = null, byte[] data = null) 
        {
            if (!Extensions.CompareBuffers(Data.Array, Data.Offset, header, 0, header.Length))
                return false;
            if (command.HasValue && Command != command.Value) return false;
            if (data != null && !Extensions.CompareBuffers(Data.Array, Data.Offset + DataOffset, data, 0, data.Length))
                return false;
            return true; 
        } 
    }
}

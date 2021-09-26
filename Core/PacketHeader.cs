using System;

namespace DgusDude.Core
{
    public struct PacketHeader
    {
        public readonly ArraySegment<byte> Data;
        public readonly ConnectionConfig Config;
        public const int FixedHeaderSize = 1 /*packet len*/ + 1 /*command*/+ 2 /*address*/;
        private byte GetByte(int index) { return Data.Array[Data.Offset + index]; }
        private void SetByte(int index, byte value) { Data.Array[Data.Offset + index] = value; }
        public PacketHeader(ConnectionConfig config, ArraySegment<byte> data) { Config = config; Data = data; }
        public PacketHeader(Device device, byte command) :
            this(device.Config, new ArraySegment<byte>(new byte[device.Config.Header.Length + FixedHeaderSize]))
        {
            if (Config.Header != null)
                Array.Copy(Config.Header, 0, Data.Array, Data.Offset, Config.Header.Length);
            Command = command;
        }
        public int HeaderLength { get { return Config.Header.Length; } }
        public bool HeaderValid { get { for (var i = 0; i < HeaderLength; i++) if (Config.Header[i] != Data.Array[Data.Offset + i]) return false; return true; } }
        public byte PacketLength { get { return GetByte(HeaderLength); } set { SetByte(HeaderLength, value); } }
        public byte DataLength { get { return (byte)(PacketLength - (FixedHeaderSize - 1)); } set { PacketLength = (byte)(value + (FixedHeaderSize - 1)); } }
        public byte Command { get { return GetByte(HeaderLength + 1); } set { SetByte(HeaderLength + 1, value); } }
        public ushort Address
        {
            set { ((uint)value).ToLittleEndien(Data.Array, Data.Offset + Config.Header.Length + 2, 2); }
            get { return (ushort)Data.Array.FromLittleEndien(Data.Offset + Config.Header.Length + 2, 2); }
        }
    }
}

using System;

namespace DgusDude
{
    public class UserPacket : Core.PackedData
    {
        public UserPacket(Core.IDeviceAccessor mem, int address, int size) 
            : base(mem, address, new byte[size], false, false) 
        { }

        public void SetValue(int position, byte value) { Data[position] = value; }
        public void SetValue(int position, short value) { ((int)value).ToLittleEndien(Data, position, 2); }
        public void SetValue(int position, int value) { ((int)value).ToLittleEndien(Data, position, 4); }
        public void SetValue(int position, long value) { ((int)value).ToLittleEndien(Data, position, 8); }
        public void SetValue(int position, float value) { ((int)value).ToLittleEndien(Data, position, 4); }
        public void SetValue(int position, double value) { ((int)value).ToLittleEndien(Data, position, 8); }

        public byte GetByte(int position) { return Data[position]; }
        public short GetShort(int position) { return (short)Data.FromLittleEndien(position, 2); }
        public int GetInt(int position) { return (int)Data.FromLittleEndien(position, 4); }
        public long GetLong(int position) { return (long)Data.FromLittleEndien(position, 8); }
        public float GetFloat(int position) { return (float)Data.FromLittleEndien(position, 4); }
        public double GetDouble(int position) { return (double)Data.FromLittleEndien(position, 8); }
    }
}

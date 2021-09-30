using System;

namespace DgusDude.Core
{
    public abstract class PackedData
    {
        public readonly IDeviceAccessor Memory;
        public readonly byte[] Data;
        public readonly int Address;
        public bool IsChanged { get; protected set; }
        public bool AutoUpdate { get; set; }
        protected virtual void Update(int offset, int length) { Memory.Write(Address, new ArraySegment<byte>(Data, offset, length)); }
        protected virtual void Refresh(int offset, int length) { Memory.Read(Address, new ArraySegment<byte>(Data, offset, length)); }
        public void Update() { Update(0, Data != null ? Data.Length : 0); IsChanged = false; }
        public void Refresh() { Refresh(0, Data != null ? Data.Length : 0); IsChanged = false; }
        protected void Changed(int offset, int length)
        {
            IsChanged = true;
            if (!AutoUpdate) return;
            Update(offset, length);
            IsChanged = false;
        }
        protected PackedData(IDeviceAccessor mem, int address, byte[] data, bool refresh = true, bool autoUpdate = false)
        {
            Memory = mem; Address = address;
            Data = data; AutoUpdate = autoUpdate;
            if (refresh) Refresh();
        }
    }
}

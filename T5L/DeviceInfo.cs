using System;

namespace DgusDude.T5L
{
    using Core;
    public class DeviceInfo : T5.DeviceInfo
    {
        public DeviceInfo(Device device) : base(device) { }
        public Tuple<uint, uint> ScreenSize 
        {
            get {
                var ret = Device.VP.Read(0x7A, 4);
                return new Tuple<uint, uint>((uint) ret.FromLittleEndien(0, 2), (uint) ret.FromLittleEndien(2, 2));
            }
        }
    }
}

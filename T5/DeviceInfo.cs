using System;
using System.Collections.Generic;
using System.Text;

namespace DgusDude.Core.T5
{
    public class DeviceInfo : IDeviceInfo
    {
        protected Device Device;
        public DeviceInfo(Device device) { Device = device; }
        public Tuple<byte, byte> Version
        {
            get
            {
                var ret = Device.VP.Read(0x0F, 2);
                return new Tuple<byte, byte>(ret[0], ret[1]);
            }
        }
        public bool IsIdle => Device.VP.Read(0x15, 2).FromLittleEndien(0, 2) == 0;
        public float Vcc => Device.VP.Read(0x30, 2).FromLittleEndien(0, 2);
        public virtual float CpuTemprature => throw new NotImplementedException();
        //led Now
        public string SDUploadFolder => System.Text.Encoding.ASCII.GetString(Device.VP.Read(0x7C, 8));
        public virtual ulong DeviceID => throw new NotImplementedException();
    }
}

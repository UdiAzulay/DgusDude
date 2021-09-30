using System;
using System.Text;

namespace DgusDude.T5
{
    public class DeviceInfo
    {
        public readonly Device Device;
        public DeviceInfo(Device device) { Device = device; }
        public bool IsIdle => Device.VP.Read(0x15, 2).FromLittleEndien(0, 2) == 0;
        public decimal Vcc => Device.VP.Read(0x30, 2).FromLittleEndien(0, 2) / 10000m;
        public decimal CpuTemprature =>
            (Device.Platform & Platform.PlatformMask) != Platform.UID1 ? 0 :
                Device.VP.Read(0x37, 2).FromLittleEndien(0, 2) / 10m;
        //led Now
        public string SDUploadDir => Encoding.ASCII.GetString(Device.VP.Read(0x7C, 8));
        public ulong DeviceID =>
            (Device.Platform & Platform.ProcessorMask) != Platform.T5 ? 0 :
                (ulong)Device.VP.Read(0x00, 8).FromLittleEndien(0, 8);
    }
}

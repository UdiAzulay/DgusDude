using System;
using System.Text;

namespace DgusDude.T5
{
    using Core;
    public class DeviceInfo
    {
        protected readonly Device Device;
        public DeviceInfo(Device device) { Device = device; }

        public ulong DeviceID =>
            (Device.Platform & Platform.ProcessorMask) != Platform.T5 ? 0 :
                (ulong)Device.VP.Read(0x00, 8).FromLittleEndien(0, 8);

        public Tuple<byte, byte> Version
        {
            get { 
                var ret = Device.VP.Read(0x0F, 2);
                return new Tuple<byte, byte>(ret[0], ret[1]); 
            }
        }

        public DateTime Time
        {
            get {
                var ret = Device.VP.Read(0x10, 8);
                if (ret[1] == 0 || ret[2] == 0) return DateTime.MinValue;
                return new DateTime(2000 + ret[0], ret[1], ret[2], ret[4], ret[5], ret[6]);
            }
            set {
                if ((Device.Platform & Platform.RTC) != 0)
                {
                    Device.VP.Write(0x9C, new byte[] {
                        0x5A, 0xA5,
                        (byte)(value.Year % 100), (byte)value.Month, (byte)value.Day,
                        (byte)value.Hour, (byte)value.Minute, (byte)value.Second
                    });
                }
                else
                {
                    Device.VP.Write(0x10, new byte[] {
                        (byte)(value.Year % 100), (byte)value.Month, (byte)value.Day, (byte)value.DayOfWeek,
                        (byte)value.Hour, (byte)value.Minute, (byte)value.Second, 0x00
                    });
                }
            }
        }
        
        public bool IsIdle => Device.VP.Read(0x15, 2).FromLittleEndien(0, 2) == 0;
        public decimal Vcc => (decimal)(Device.VP.Read(0x30, 2).FromLittleEndien(0, 2) * (4800 / 65532.0)) / 1000m;
        public decimal CpuTemprature =>
            (Device.Platform & Platform.PlatformMask) != Platform.UID1 ? 0 :
                (Device.VP.Read(0x37, 2).FromLittleEndien(0, 2) * (240 / 929m)) * 10;
        //led Now
        public string SDUploadDir => Encoding.ASCII.GetString(Device.VP.Read(0x7C, 8));
    }
}

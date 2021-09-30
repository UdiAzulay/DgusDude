using System;

namespace DgusDude.K600
{
    using Core;
    public class DeviceInfo
    {
        public readonly Device Device;
        public DeviceInfo(Device device) { Device = device; }
        public byte Version => Device.VP.Read(0x00, 1)[0];
        public byte Brightness { get { return Device.VP.Read(0x01, 1)[0]; } set { Device.VP.Write(0x01, new[] { value }); } }
        public DateTime Time
        {
            get
            {
                var ret = Device.VP.Read(0x20, 16);
                if (ret[1] == 0 || ret[2] == 0) return DateTime.MinValue;
                return new DateTime(2000 + ret[0].FromBCD(), ret[1].FromBCD(), ret[2].FromBCD(), ret[4].FromBCD(), ret[5].FromBCD(), ret[6].FromBCD());
            }
            set
            {
                var hasRTC = (Device.Platform & Platform.RTC) != 0;
                Device.VP.Write(0x20, new byte[] {
                    (byte)(hasRTC ? 0x5A : 0x00),
                    (value.Year % 100).ToBCD(), value.Month.ToBCD(), value.Day.ToBCD(), ((int)value.DayOfWeek).ToBCD(),
                    value.Hour.ToBCD(), value.Minute.ToBCD(), value.Second.ToBCD(), 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                });
            }
        }
    }
}

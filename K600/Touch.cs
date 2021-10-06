using System;

namespace DgusDude.K600
{
    public class Touch : Core.Touch
    {
        public Touch(Device device) : base(device) { }
        public override TouchStatus Current => new TouchStatus(Device.VP.Read(0x05, 7));
    }
}

using System;
namespace DgusDude.Core.T5
{
    public abstract class T5 : T5Base
    {
        public T5(ConnectionConfig config) : base(config)
        {
            //REG memory addresses are 0:0x8FF
            Register = new RegisterAccessor(this, 0x900); //2k Registers
            //SRAM memory addresses are 0:0x1FFFF
            SRAM = new SRAMAccessor(this, 0x20000); //128kb
            //320kb - [nor addresses - 3 byte: 0x000000-0x02:7FFE]
            Nor = new NorAccessor(this, 0x50000); //320kb 3FFF0
        }
        protected class T5DeviceInfo : DeviceInfo
        {
            public T5DeviceInfo(T5 device) : base(device) { }
            public override ulong DeviceID => Device.VP.Read(0x00, 8).FromLittleEndien(0, 8);
        }
    }
}

using System;

namespace DgusDude.T5
{
    using Core;
    public class Touch : Core.Touch
    {
        public Touch(Device device) : base(device) { }
        
        public override TouchStatus Current => new TouchStatus(Device.VP.Read(0x16, 8));
        
        public override void Simulate(TouchAction action, uint x, uint y)
        {
            var val = new byte[8] { 0x5A, 0xA5, 00, (byte)action, 0, 0, 0, 0 };
            ((int)x).ToLittleEndien(val, 4, 2);
            ((int)y).ToLittleEndien(val, 6, 2);
            Device.VP.Write(0xD4, val);
            Device.VP.Wait(0xD4);
        }

        public override void EnableControl(int pictureId, int controlId, bool value)
        {
            var controlIdBytes = controlId.ToLittleEndien(2);
            Device.VP.Write(0xB0, new byte[] { 
                0x5A, 0xA5,     //fixed
                (byte)pictureId, 
                controlIdBytes[0], controlIdBytes[1],
                0x00, (byte)(value ? 0x01 : 0x00),  //access mode
            });
            Device.VP.Wait(0xB0);
        }
    }
}

using System;

namespace DgusDude.T5
{
    public class SystemConfig : Core.PackedData
    {
        [Flags]
        public enum StatusBits : byte
        {
            LCDFlipLR = 0x01, LCDFlipVH = 0x02, StandByBacklight = 0x04, TouchTone = 0x08,
            SDEnabled = 0x10, InitWith22Config = 0x20, DisplayControlPage1 = 0x40, /*Reserved*/ CheckCRC = 0x80,
        }
        public SystemConfig(T5Device device, bool refresh = true, bool autoUpdate = true) : base(device.VP, 0x80, new byte[4], refresh, autoUpdate) { }
        public byte TouchSensitivity { get { return Data[1]; } }
        public byte TouchMode { get { return Data[2]; } }
        public StatusBits Status { get { return (StatusBits)Data[3]; } set { Data[3] = (byte)value; Changed(0, Data.Length); } }
        protected override void Update(int offset, int length) { base.Update(0, Data.Length); }
        public override string ToString() { return string.Format("{0}, Touch Mode: {1} Sensitivity: {2}", Status, TouchMode, TouchSensitivity); }
    }
}

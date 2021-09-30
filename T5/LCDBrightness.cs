using System;

namespace DgusDude.T5
{
    using Core;
    public class LCDBrightness : Core.PackedData
    {
        public LCDBrightness(T5Device device, bool refresh = true, bool autoUpdate = true) : base(device.VP, 0x82, new byte[4], refresh, autoUpdate) { }
        public byte Normal { get { return Data[0]; } set { Data[0] = value; Changed(0, 2); } }
        public byte StandBy { get { return Data[1]; } set { Data[1] = value; Changed(0, 2); } }
        public uint StandByTime { get { return (uint)Data.FromLittleEndien(2, 2); } set { ((int)value).ToLittleEndien(Data, 2, 2); Changed(2, 2); } }
        public override string ToString() { return string.Format("Normal: {0}, StandBy: {1} after: {2}ms", Normal, StandBy, StandByTime); }
    }
}

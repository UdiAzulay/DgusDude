using System;

namespace DgusDude.Core
{
    public class ModelNumber
    {
        public readonly string Value;
        private ModelNumber(string value) { Value = value; }
        public string Header { get { return Value.Substring(0, 2); } }
        public char PixelRes { get { return Value[2]; } }
        public string LCDSize { get { return Value.Substring(3, 5); } }
        public char Certificate { get { return Value[8]; } }
        public string LCDInch { get { return Value.Substring(9, 3); } }
        public char FixedUnderscore { get { return Value[12]; } }

        public string HardwareSerial { get { return Value.Substring(13, 2); } }
        public char FixedWChar { get { return Value[15]; } }
        public char Touch { get { return Value.Length <= 16 ? (char)0 : Value[16]; } }

        private readonly static System.Collections.Generic.Dictionary<string, Tuple<uint, uint>> LCDSizeMap =
            new System.Collections.Generic.Dictionary<string, Tuple<uint, uint>>
            {
                { "32240", new Tuple<uint, uint>(320, 240) },
                { "48270", new Tuple<uint, uint>(480, 272) },
                { "64480", new Tuple<uint, uint>(640, 480) },
                { "80480", new Tuple<uint, uint>(800, 480) },
                { "85480", new Tuple<uint, uint>(854, 480) },
                { "80600", new Tuple<uint, uint>(800, 600) },
                { "10600", new Tuple<uint, uint>(1024, 600) },
                { "10768", new Tuple<uint, uint>(1024, 768) },
                { "12720", new Tuple<uint, uint>(1280, 720) },
                { "12800", new Tuple<uint, uint>(1280, 800) },
                { "13768", new Tuple<uint, uint>(1364, 768) },
                { "19108", new Tuple<uint, uint>(1920, 1080) },
            };

        public Core.Screen CreateLCD()
        {
            Tuple<uint, uint> lcdSize;
            if (!LCDSizeMap.TryGetValue(LCDSize, out lcdSize))
                throw new System.Exception("Unrecognized screen size code");
            var inchSize = decimal.Parse(LCDInch) / 10;
            var pixelformat = System.Drawing.Imaging.PixelFormat.Undefined;
            switch (PixelRes)
            {
                case 'T': pixelformat = System.Drawing.Imaging.PixelFormat.Format16bppRgb565; break;
                case 'G': pixelformat = System.Drawing.Imaging.PixelFormat.Format24bppRgb; break;
            }
            return new Screen(lcdSize.Item1, lcdSize.Item2, pixelformat, inchSize);
        }

        public bool HasTouch => Touch != 'N' && Touch != (char)0;
        public Platform Platform => Platform.T5 | (HasTouch? Platform.TouchScreen : 0);
        private bool IsValid => (Header == "DM" && FixedUnderscore == '_' && FixedWChar == 'W');
        public static ModelNumber Parse(string value) 
        {
            var ret = new ModelNumber(value); 
            if (!ret.IsValid) throw new Exception("device model format is DMxnnnnnxnnn_xnWx");
            return ret;
        }

    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DgusDude.Devices
{
    public class T5UIDx : Core.T5.T5, IRTC
    {
        public const ushort DWIN_ACK_VALUE = 0x4F4B;
        public T5UIDx(Core.ConnectionConfig config) : base(config) { }
        public override T GetInterface<T>()
        {
            var tt = typeof(T);
            if (tt == typeof(IRTC)) return this as T;
            else if (tt == typeof(ITouchStatus)) return Touch as T;
            else if (tt == typeof(IPictureStorage)) return Pictures as T;
            else if (tt == typeof(IMusicStorage)) return Music as T;
            else if (tt == typeof(IBacklightControl)) return Music as T;
            else if (tt == typeof(SystemConfig)) return DeviceConfig as T;
            else return base.GetInterface<T>();
        }
        
        public class TouchStatus : Core.PackedData, ITouchStatus
        {
            public TouchStatus(T5UIDx device, bool refresh = true) : base(device.VP, 0x16, new byte[8], refresh, false) { }
            public TouchAction Status { get; private set; }
            public uint X { get; private set; }
            public uint Y { get; private set; }
            protected override void Refresh(int offset, int length)
            {
                base.Refresh(0, Data.Length);
                Status = (TouchAction)Data[1];
                X = (uint)Data.FromLittleEndien(2, 2);
                Y = (uint)Data.FromLittleEndien(4, 2);
            }
            protected override void Update(int offset, int length) { throw new DWINException("update of touch is not supported, use simulate instead"); }
            public void Simulate(TouchAction action, uint x, uint y)
            {
                var val = new byte[8] { 0x5A, 0xA5, 00, (byte)action, 0, 0, 0, 0 };
                x.ToLittleEndien(val, 4, 2);
                y.ToLittleEndien(val, 6, 2);
                Memory.Write(0xD4, new ArraySegment<byte>(val));
                (Memory as Core.VP).Wait(0xD4);
            }
        }
        public TouchStatus Touch => new TouchStatus(this);

        public class SystemConfig : Core.PackedData
        {
            [Flags]
            public enum StatusBits : byte
            {
                LCDFlipLR = 0x01, LCDFlipVH = 0x02, StandByBacklight = 0x04, TouchTone = 0x08,
                SDEnabled = 0x10, InitWith22Config = 0x20, DisplayControlPage1 = 0x40, /*Reserved*/ CheckCRC = 0x80,
            }
            public SystemConfig(T5UIDx device, bool refresh = true, bool autoUpdate = true) : base(device.VP, 0x80, new byte[4], refresh, autoUpdate) { }
            public byte TouchSensitivity { get { return Data[1]; } }
            public byte TouchMode { get { return Data[2]; } }
            public StatusBits Status { get { return (StatusBits)Data[3]; } set { Data[3] = (byte)value; Changed(0, Data.Length); } }
            protected override void Update(int offset, int length) { base.Update(0, Data.Length); }
        }
        public SystemConfig DeviceConfig => new SystemConfig(this);

        public class LCDBrightness : Core.PackedData, IBacklightControl
        {
            public LCDBrightness(T5UIDx device, bool refresh = true, bool autoUpdate = true) : base(device.VP, 0x82, new byte[4], refresh, autoUpdate) { }
            public byte Normal { get { return Data[0]; } set { Data[0] = value; Changed(0, 2); } }
            public byte StandBy { get { return Data[1]; } set { Data[1] = value; Changed(0, 2); } }
            public uint StandByTime { get { return (uint)Data.FromLittleEndien(2, 2); } set { value.ToLittleEndien(Data, 2, 2); Changed(2, 2); } }
        }
        public LCDBrightness Brightness => new LCDBrightness(this);

        public bool HasHardwareRTC { get; private set; }
        public DateTime Time
        {
            get
            {
                var ret = VP.Read(0x10, 8);
                if (ret[1] == 0 || ret[2] == 0) return DateTime.MinValue;
                return new DateTime(2000 + ret[0], ret[1], ret[2], ret[4], ret[5], ret[6]);
            }
            set {
                if (HasHardwareRTC) {
                    VP.Write(0x9C, new byte[] {
                        0x5A, 0xA5,
                        (byte)(value.Year % 100), (byte)value.Month, (byte)value.Day,
                        (byte)value.Hour, (byte)value.Minute, (byte)value.Second
                    });
                } else {
                    VP.Write(0x10, new byte[] {
                        (byte)(value.Year % 100), (byte)value.Month, (byte)value.Day, (byte)value.DayOfWeek,
                        (byte)value.Hour, (byte)value.Minute, (byte)value.Second, 0x00
                    });
                }
            }
        }

        public class PictureStorage : IPictureStorage //, Core.IDrawPicture
        {
            public readonly Device Device;
            public uint Length { get; private set; }
            public System.Drawing.Imaging.Encoder[] SupportedEncoders { get; private set; }
            public PictureStorage(Device device, uint length) { 
                Device = device; Length = length; 
            }

            //upload 16 bit per pixel data
            private static void SwapFileBytes(byte[] data, int startIndex, int length)
            {
                byte tmp;
                for (var i = startIndex; i < (startIndex + length); i += 2) {
                    tmp = data[i]; data[i] = data[i + 1]; data[i + 1] = tmp;
                }
            }

            private void Upload_Bitmap(byte[] data, bool swapBytes = false, bool verify = false)
            {
                uint offset = 0;
                var sramAddress = (Device.BufferAddress >> 1).ToLittleEndien(2);
                if (swapBytes) SwapFileBytes(data, 0, data.Length);
                foreach (var v in new Core.Slicer(new ArraySegment<byte>(data), (uint)Device.BufferLength))
                {
                    Device.SRAM.Write(Device.BufferAddress, v, verify);
                    var dataLength = ((uint)v.Count >> 1).ToLittleEndien(2);
                    var imagePosition = (offset >> 1).ToLittleEndien(3);
                    Device.VP.Write(0xA2, new byte[] {
                        0x5A, //fixed
                        sramAddress[0], sramAddress[1], //SRAM position
                        dataLength[0], dataLength[1], //data length in words
                        imagePosition[0], imagePosition[1], imagePosition[2] //image buffer position
                    });
                    Device.VP.Wait(0xA2);
                    offset += (uint)v.Count;
                }
            }

            private void Upload_Jpg(byte[] data, bool modeSave, ushort modeParam, bool verify = false)
            {
                if (data.Length > Device.BufferLength) throw new Exception("buffer size is too small, use BufferLength to enlarge it");
                var sramAddress = (Device.BufferAddress >> 1).ToLittleEndien(2);
                Device.SRAM.Write(Device.BufferAddress, new ArraySegment<byte>(data), verify);
                var picIdOrPos = ((uint)modeParam).ToLittleEndien(2);
                Device.VP.Write(0xA6, new byte[] {
                    0x5A, //fixed
                    (byte)(modeSave ? 0x02 : 0x01), //display
                    sramAddress[0], sramAddress[1], //sram position
                    picIdOrPos[0], picIdOrPos[1], //screen position or pictureId
                    0, 0 //undocumented, set 0
                });
                Device.VP.Wait(0xA6);
            }

            public uint Current
            {
                get { return (uint)Device.VP.Read(0x14, 2).FromLittleEndien(0, 2); }
                set
                {
                    var picIdBytes = value.ToLittleEndien(2);
                    Device.VP.Write(0x84, new byte[] { 0x5A, 0x01, picIdBytes[0], picIdBytes[1] });
                    Device.VP.Wait(0x84);
                }
            }

            public void TakeScreenshot(uint pictureId)
            {
                var picIdBytes = pictureId.ToLittleEndien(2);
                Device.VP.Write(0x84, new byte[] {
                    0x5A, //fixed
                    0x02, //save picture
                    picIdBytes[0], picIdBytes[1]
                });
            }
            
            public void UploadPicture(uint pictureId, System.IO.Stream stream, ImageFormat format, bool verify)
            {
                if (format == System.Drawing.Imaging.ImageFormat.Bmp)  {
                    using (var image = Image.FromStream(stream)) 
                        Upload_Bitmap(new Bitmap(image).GetBytes(PixelFormat.Format16bppRgb565), true, verify);
                    TakeScreenshot(pictureId);
                } else if (format == System.Drawing.Imaging.ImageFormat.Jpeg) {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    Upload_Jpg(data, true, (ushort)pictureId, verify);
                }
            }
        }
        public PictureStorage Pictures { get; protected set; }

        public class MusicStorage : IMusicStorage
        {
            public Core.MemoryAccessor Memory { get; private set; }
            public uint Length { get; private set; }
            public readonly uint StorageOffset;
            public const uint BlockSize = 0x020000; //128Kb

            public MusicStorage(Core.MemoryAccessor mem, uint length, uint storageOffset) { Memory = mem; Length = length; StorageOffset = storageOffset; }
            public void Play(int start, int length, byte volume = 0xFF)
            {
                Memory.Device.VP.Write(0xA0, new byte[] { (byte)start, (byte)length, volume, 0x00 });
            }
            public void Stop() => Play(0, 0, 0);

            /* each section is 2.048sec at 32KHz 16bit      */
            /* (2 bytes * 32000 * 2.048 = 131072 = 0x20000) */
            public void Upload_Wave(string fileName, byte startSection, bool verify = false)
            {
                byte[] data = System.IO.File.ReadAllBytes(fileName);
                var address = (startSection * BlockSize) + StorageOffset;
                int channels = data[22];
                if (channels != 1) throw new Exception("wav file incorrect format, only support 1 channel of 16bit at 32KHz");
                uint chunkSize, pos = 12;   // First Subchunk ID from 12 to 16
                //finr DATA chunk
                while (!(data[pos] == 100 && data[pos + 1] == 97 && data[pos + 2] == 116 && data[pos + 3] == 97))
                {
                    pos += 4;
                    chunkSize = (uint)data.FromBigEndien((int)pos, 4);
                    pos += 4 + (uint)chunkSize;
                }
                chunkSize = (uint)data.FromBigEndien((int)pos + 4, 4);
                pos += 8;
                //pos = 0;
                //pos += 100; chunkSize -= 100; /*skip forword*/
                Memory.Write(address, new ArraySegment<byte>(data, (int)pos, (int)chunkSize), verify);
            }

            public void UploadWave(uint waveId, byte[] samples)
            {
                throw new NotImplementedException();
            }

            public void UploadWave(uint waveId, string fileName)
            {
                throw new NotImplementedException();
            }
        }
        public MusicStorage Music { get; protected set; }

    }
}

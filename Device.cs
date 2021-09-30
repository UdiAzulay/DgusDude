using System;
using System.Linq;
using System.IO.Ports;

namespace DgusDude
{
    using Core;

    [Flags]
    public enum Platform
    {
        ProcessorMask = 0x07, PlatformMask = 0x70,
        K600 = 0x01, T5 = 0x02, T5L = 0x03,
        RTC = 0x08,
        UID1 = 0x10, UID2 = 0x20, UID3 = 0x30,
        TouchScreen = 0x80
    };

    public abstract class Device : IDisposable
    {
        public const byte MAX_PACKET_SIZE = 0xF8;
        private bool _abort = false;

        public Platform Platform { get; private set; }
        public Screen Screen { get; private set; }
        public ConnectionConfig Config { get; private set; }
        public SerialPort SerialPort { get; private set; }

        public MemoryAccessor Registers { get; protected set; }
        public MemoryAccessor RAM { get; protected set; }
        public MemoryBufferedAccessor Storage { get; protected set; }
        public MemoryBufferedAccessor UserSettings { get; protected set; }
        public MemoryBuffer Buffer { get; protected set; }
        public VP VP { get; protected set; }

        public PictureStorage Pictures { get; protected set; }
        public MusicStorage Music { get; protected set; }

        public event EventHandler<DataEventArgs> DataRead;
        public event EventHandler<DataEventArgs> DataWrite;

        public Device(Platform platform, Screen screen)
        {
            Platform = platform;
            Screen = screen;
            Config = new ConnectionConfig();
            SerialPort = new SerialPort()
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One, //Two
                ReadTimeout = 3000,
                WriteTimeout = 3000
            };
        }

        ~Device() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing)
        {
            if (SerialPort == null) return;
            if (SerialPort.IsOpen) SerialPort.Close();
            SerialPort.Dispose();
        }

        public static Device Create(Platform platform, Screen screen, uint? flashSize = null)
        {
            if ((platform & Platform.ProcessorMask) == Platform.K600)
                return new K600.K600Device(platform, screen, flashSize);
            else return new T5.T5Device(platform, screen, flashSize);
        }

        public void Open(string portName, int? baudRate = null, bool? twoStopBits = null)
        {
            if (!string.IsNullOrEmpty(portName)) SerialPort.PortName = portName;
            if (baudRate.HasValue) SerialPort.BaudRate = baudRate.Value;
            if (twoStopBits.HasValue) SerialPort.StopBits = twoStopBits.Value ? StopBits.Two : StopBits.One;
            SerialPort.Open();
        }
        public void Close() => SerialPort.Close();
        public void Abort() => _abort = true;

        //good reset sequence is: 5A, A5, 07, 82, 00, 04, 55, AA, 5A, 5A
        public void RawWrite(int retry, params ArraySegment<byte>[] buffers)
        {
            int globalOffset = 0;
            var totalBytes = buffers.Sum(v => v.Count);
            //if (totalBytes > DWIN_REG_MAX_RW_BLEN) throw new DWINException("DWIN_Write length cannot exceed " + DWIN_REG_MAX_RW_BLEN);
            System.Exception exNotify = null;
            try {
                _abort = false;
                foreach (var v in buffers)
                {

/* Unmerged change from project 'DgusDude (net5.0)'
Before:
                    if (_abort) throw new DWINException("Operation aborted");
After:
                    if (_abort) throw new DgusDude.DWINException("Operation aborted");
*/

/* Unmerged change from project 'DgusDude (netstandard2.0)'
Before:
                    if (_abort) throw new DWINException("Operation aborted");
After:
                    if (_abort) throw new DgusDude.DWINException("Operation aborted");
*/
                    if (_abort) throw new Exception("Operation aborted");
                    SerialPort.Write(v.Array, v.Offset, v.Count);
                    DataWrite?.Invoke(this, new DataEventArgs(true, v, globalOffset, totalBytes, retry));
                    globalOffset += v.Count;
                }
            } catch (System.Exception ex) {
                exNotify = ex;
                throw;
            } finally {
                DataWrite?.Invoke(this, new DataEventArgs(true, Extensions.EmptyArraySegment, globalOffset, totalBytes, retry, exNotify));
            }
        }
        public void RawRead(int retry, params ArraySegment<byte>[] buffers)
        {
            int globalOffset = 0;
            var totalBytes = buffers.Sum(v => v.Count);
            if (totalBytes > 0xFF) throw new Exception("DWIN_Read length cannot exceed " + 0xFF);
            System.Exception exNotify = null;
            try {
                _abort = false;
                foreach (var v in buffers)
                {
                    var offset = 0;
                    var keepBytes = 0; //min bytes begore first notify
                    while (offset < v.Count)
                    {

/* Unmerged change from project 'DgusDude (net5.0)'
Before:
                        if (_abort) throw new DWINException("Operation aborted");
After:
                        if (_abort) throw new DgusDude.DWINException("Operation aborted");
*/

/* Unmerged change from project 'DgusDude (netstandard2.0)'
Before:
                        if (_abort) throw new DWINException("Operation aborted");
After:
                        if (_abort) throw new DgusDude.DWINException("Operation aborted");
*/
                        if (_abort) throw new Exception("Operation aborted");
                        var bytesLeft = v.Count - offset;
                        var byteRead = SerialPort.Read(v.Array, v.Offset + offset, bytesLeft);
                        if (offset + byteRead >= 6)
                        {
                            DataRead?.Invoke(this, new DataEventArgs(false, new ArraySegment<byte>(v.Array, v.Offset + offset - keepBytes, byteRead + keepBytes), globalOffset, totalBytes, retry));
                            globalOffset += byteRead + keepBytes;
                            keepBytes = 0;
                        }
                        else keepBytes += byteRead;
                        offset += byteRead;
                    }
                    if (keepBytes > 0)
                    { //ensure notification
                        DataRead?.Invoke(this, new DataEventArgs(false, new ArraySegment<byte>(v.Array, v.Offset + offset - keepBytes, keepBytes), globalOffset, totalBytes, retry));
                        globalOffset += keepBytes;
                    }
                }
            } catch (System.Exception ex) {
                exNotify = ex;
                throw;
            } finally {
                DataRead?.Invoke(this, new DataEventArgs(false, Extensions.EmptyArraySegment, globalOffset, totalBytes, retry, exNotify));
            }
        }

        public bool Connected => SerialPort.IsOpen;

        public abstract void Reset(bool cpuOnly);
        public abstract void Format(Action<int> progress = null);

        protected abstract void UploadBin(int index, byte[] data, bool verify = false);
        public virtual bool Upload(string fileName, bool verify = false)
        {
            var fileNameOnly = System.IO.Path.GetFileName(fileName);
            int fileIndex = int.MaxValue;
            for (var i = 0; i < fileNameOnly.Length; i++)
            {
                if (char.IsDigit(fileNameOnly[i])) continue;
                if (i > 0) fileIndex = int.Parse(fileNameOnly.Substring(0, i));
                break;
            }
            var ext = System.IO.Path.GetExtension(fileName)?.ToUpper()?.TrimStart('.');
            switch (ext)
            {
                case "JPG":
                    using (var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        Pictures.UploadPicture(fileIndex, f, System.Drawing.Imaging.ImageFormat.Jpeg, verify);
                    return true;
                case "BMP":
                    using (var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        Pictures.UploadPicture(fileIndex, f, System.Drawing.Imaging.ImageFormat.Bmp, verify);
                    return true;
                case "WAV":
                    using (var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        Music.Upload(fileIndex, f, verify);
                    return true;
                case "HZK":
                case "DZK":
                case "BIN":
                case "ICO":
                    UploadBin(fileIndex, System.IO.File.ReadAllBytes(fileName));
                    return true;
                case "LIB":
                    if (fileIndex < 0 || fileIndex > 80) throw Exception.CreateFileIndex(fileName);
                    UserSettings.Write((int)(fileIndex * 0x1000u), new ArraySegment<byte>(System.IO.File.ReadAllBytes(fileName)), verify);
                    return true;
            }
            return false;
        }

        public virtual TouchStatus GetTouch() { return null; }
        public UserPacket ReadKey(int timeout = -1)
        {
            if ((Platform & Platform.TouchScreen) != Platform.TouchScreen)
                throw new NotSupportedException();
            var readTimeout = SerialPort.ReadTimeout;
            var header = new PacketHeader((VP.Memory as MemoryDirectAccessor).AddressMode, Config.Header.Length, 0);
            SerialPort.ReadTimeout = timeout;
            try {
                RawRead(0, header.Data);
                var ret = new UserPacket(RAM, header.Address, header.DataLength);
                RawRead(0, new ArraySegment<byte>(ret.Data));
                return ret;
            } catch (TimeoutException) {
                return null;
            } finally {
                SerialPort.ReadTimeout = readTimeout;
            }
        }

    }
}

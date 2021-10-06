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
        K600 = 0x01, K600Mini = 2,
        T5 = 0x04, T5L1 = 0x05, T5L2 = 0x06,
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

        public MemoryAccessor Registers { get; protected set; }
        public MemoryAccessor RAM { get; protected set; }
        public MemoryBufferedAccessor Storage { get; protected set; }
        public MemoryBufferedAccessor UserSettings { get; protected set; }
        public MemoryBuffer Buffer { get; protected set; }
        public VP VP { get; protected set; }

        public PictureStorage Pictures { get; protected set; }
        public MusicStorage Music { get; protected set; }

        public Touch Touch { get; protected set; }
        public PWM PWM { get; protected set; }
        public ADC ADC { get; protected set; }

        public ConnectionConfig Config { get; private set; }
        public SerialPort Connection { get; private set; }
        public bool Connected => Connection.IsOpen;

        public event EventHandler<DataEventArgs> DataRead;
        public event EventHandler<DataEventArgs> DataWrite;

        protected Device(Platform platform, Screen screen)
        {
            Platform = platform;
            Screen = screen;
            Config = new ConnectionConfig();
            Connection = new SerialPort()
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
            if (Connection == null) return;
            if (Connection.IsOpen) Connection.Close();
            Connection.Dispose();
        }

        public static Device Create(Platform platform, Screen screen, uint? flashSize = null)
        {
            switch(platform & Platform.ProcessorMask) {
                default: 
                    throw new ArgumentException("platform");
                case Platform.K600:
                case Platform.K600Mini:
                    return new K600.K600Device(platform, screen, flashSize);
                case Platform.T5: 
                    return new T5.T5Device(platform, screen, flashSize);
                case Platform.T5L1:
                case Platform.T5L2: 
                    return new T5L.T5LDevice(platform, screen, flashSize);
            }
        }

        public static Device Create(string modelNumber, uint? flashSize = null)
        {
            var model = ModelNumber.Parse(modelNumber);
            return Create(model.Platform, model.CreateLCD(), flashSize);
        }

        public void Open(string portName, int? baudRate = null, bool? twoStopBits = null)
        {
            if (!string.IsNullOrEmpty(portName)) Connection.PortName = portName;
            if (baudRate.HasValue) Connection.BaudRate = baudRate.Value;
            if (twoStopBits.HasValue) Connection.StopBits = twoStopBits.Value ? StopBits.Two : StopBits.One;
            Connection.Open();
        }
        public void Close() => Connection.Close();
        public void Abort() => _abort = true;

        public abstract void Reset(bool cpuOnly);
        public virtual void Format(Action<int> progress = null)
        {
            if (Storage != null) Storage.MemSet(0, Storage.Length, Extensions.EmptyArraySegment, false);
            if (UserSettings != null) Storage.MemSet(0, UserSettings.Length, Extensions.EmptyArraySegment, false);
        }

        protected virtual void Upload(MemoryAccessor mem, uint pageSize, System.IO.Stream stream, int index, bool verify = false)
        {
            var maxIndex = mem.Length / pageSize;
            if (index < 0 || index > maxIndex) throw Exception.CreateOutOfRange(index, pageSize);
            var address = (int)(index * pageSize); //256kb blocks
            mem.Write(address, stream, verify);
        }
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
            var ext = System.IO.Path.GetExtension(fileName)?.TrimStart('.');
            using (var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                return Upload(f, ext, fileIndex, verify);
        }
        public virtual bool Upload(System.IO.Stream stream, string fileExt, int? index, bool verify = false) => false;

        //good reset sequence for T5 is: 5A, A5, 07, 82, 00, 04, 55, AA, 5A, 5A
        public void RawWrite(int retry, params ArraySegment<byte>[] buffers)
        {
            int globalOffset = 0;
            var totalBytes = buffers.Sum(v => v.Count);
            //if (totalBytes > DWIN_REG_MAX_RW_BLEN) throw new Exception("RawWrite length cannot exceed " + DWIN_REG_MAX_RW_BLEN);
            System.Exception exNotify = null;
            try {
                _abort = false;
                foreach (var v in buffers)
                {
                    if (_abort) throw new Exception("Operation aborted");
                    Connection.Write(v.Array, v.Offset, v.Count);
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
            if (totalBytes > 0xFF) throw new Exception("RawRead length cannot exceed " + 0xFF);
            System.Exception exNotify = null;
            try {
                _abort = false;
                foreach (var v in buffers)
                {
                    var offset = 0;
                    var keepBytes = 0; //min bytes begore first notify
                    while (offset < v.Count)
                    {
                        if (_abort) throw new Exception("Operation aborted");
                        var bytesLeft = v.Count - offset;
                        var byteRead = Connection.Read(v.Array, v.Offset + offset, bytesLeft);
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

    }
}

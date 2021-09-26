using System;
using System.Linq;
using System.IO.Ports;
using DgusDude.Core;

namespace DgusDude
{
    public abstract class Device : IDisposable
    {
        private bool _abort { get; set; } = false;
        public ConnectionConfig Config { get; private set; }
        public SerialPort SerialPort { get; private set; }
        public event EventHandler<DataEventArgs> DataRead;
        public event EventHandler<DataEventArgs> DataWrite;
        public uint BufferAddress { get; set; } = 0x10000; //0x02000 start at 0x08000 (word)
        public int BufferLength { get; set; } = 0x10000; //64k;

        public VP VP { get; protected set; }
        public MemoryAccessor SRAM { get; protected set; }
        public MemoryAccessor Register { get; protected set; }
        public MemoryBufferedAccessor Nand { get; protected set; }

        public System.Drawing.Size LCDSize { get; protected set; }
        public System.Drawing.Imaging.PixelFormat PixelFormat { get; protected set; }
        public System.Drawing.Imaging.ImageFormat[] SupportedImageFormats { get; protected set; }

        public Device(ConnectionConfig config)
        {
            SerialPort = new SerialPort()
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One, //Two
                ReadTimeout = 3000,
                WriteTimeout = 3000
            };
            Config = config ?? new ConnectionConfig();
        }

        ~Device() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing)
        {
            if (SerialPort == null) return;
            if (SerialPort.IsOpen) SerialPort.Close();
            SerialPort.Dispose();
        }

        public void Open(string portName, int? baudRate = null, bool? twoStopBits = null, bool? crc = null)
        {
            if (!string.IsNullOrEmpty(portName)) SerialPort.PortName = portName;
            if (baudRate.HasValue) SerialPort.BaudRate = baudRate.Value;
            if (twoStopBits.HasValue) SerialPort.StopBits = twoStopBits.Value ? StopBits.Two : StopBits.One;
            if (crc.HasValue) Config.EnableCRC = crc.Value;
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
            Exception exNotify = null;
            try
            {
                foreach (var v in buffers)
                {
                    if (_abort) throw new DWINException("Operation aborted");
                    SerialPort.Write(v.Array, v.Offset, v.Count);
                    DataWrite?.Invoke(this, new DataEventArgs(true, v, globalOffset, totalBytes, retry));
                    globalOffset += v.Count;
                }
            }
            catch (Exception ex)
            {
                exNotify = ex;
                throw;
            }
            finally
            {
                _abort = false;
                DataWrite?.Invoke(this, new DataEventArgs(true, Extensions.EmptyArraySegment, globalOffset, totalBytes, retry, exNotify));
            }
        }

        public void RawRead(int retry, params ArraySegment<byte>[] buffers)
        {
            int globalOffset = 0;
            var totalBytes = buffers.Sum(v => v.Count);
            if (totalBytes > 0xFF) throw new DWINException("DWIN_Read length cannot exceed " + 0xFF);
            Exception exNotify = null;
            try
            {
                foreach (var v in buffers)
                {
                    var offset = 0;
                    var keepBytes = 0; //min bytes begore first notify
                    while (offset < v.Count)
                    {
                        if (_abort) throw new DWINException("Operation aborted");
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
            }
            catch (Exception ex)
            {
                exNotify = ex;
                throw;
            }
            finally
            {
                _abort = false;
                DataRead?.Invoke(this, new DataEventArgs(false, Extensions.EmptyArraySegment, globalOffset, totalBytes, retry, exNotify));
            }
        }

        public virtual T GetInterface<T>() where T : class
        {
            throw DWINException.CreateInterfaceNotExist(typeof(T));
        }


        public abstract void Reset(bool cpuOnly);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DgusDude.Core.T5
{
    public abstract class T5Base : Device, IUpload
    {
        public const byte DWIN_REG_MAX_RW_BLEN = 0xF8;
        public MemoryBufferedAccessor Nor { get; protected set; }

        public T5Base(ConnectionConfig config) : base(config) 
        {
            VP = new T5VP(this);
        }

        public override T GetInterface<T>()
        {
            if (typeof(T) == typeof(IUpload)) return this as T;
            return base.GetInterface<T>();
        }

        protected class T5VP : VP
        {
            public T5VP(Device device) : base(device) { }
            public override void Read(uint address, ArraySegment<byte> data)
            {
                Device.SRAM.Read(address << 1, data);
            }

            public override void Write(uint address, ArraySegment<byte> data)
            {
                Device.SRAM.Write(address << 1, data, false);
                //System.Threading.Thread.Sleep(20); //give time to command to finish exec
            }
        }

        public override void Reset(bool cpuOnly = false) => VP.Write(0x04, new byte[] { 0x55, 0xAA, 0x5A, (byte)(cpuOnly ? 0xA5 : 0x5A) });

        public void OS_Update(byte[] data, byte target = 0x10, bool verify = false)
        {
            var offset = target == 0x10 ? 0x1000 : 0; //DWIN cache org
            var bufSize = (target == 0x10 ? 0x7000 /*28kb*/: 0x10000/*64kb*/) - offset;
            if (bufSize > BufferLength) throw new Exception("buffer size too small");
            if (data.Length > bufSize) throw new Exception("file too big to upload");
            var newBufffer = new byte[bufSize];
            Array.Copy(data, offset, newBufffer, 0, data.Length - offset);
            //for (var i = data.Length - offset; i < newBufffer.Length; i++) newBufffer[i] = byte.MaxValue;
            SRAM.Write(BufferAddress, new ArraySegment<byte>(newBufffer), verify);
            var sramAddress = (BufferAddress >> 1).ToLittleEndien(2);
            VP.Write(0x06, new byte[] {
                0x5A, //fixed
                target, // 0x10: DWIN OS user code - 28kb, 0x5A: 8051 code - 64kb
                sramAddress[0], sramAddress[1], //SRAM position
            });
            System.Threading.Thread.Sleep(200); //minimum wait time
            VP.Wait(0x06);
        }

        public virtual bool Upload(string fileName, bool verify = false)
        {
            var fileNameOnly = System.IO.Path.GetFileName(fileName);
            uint fileIndex = uint.MaxValue;
            for (var i = 0; i < fileNameOnly.Length; i++)
            {
                if (char.IsDigit(fileNameOnly[i])) continue;
                if (i > 0) fileIndex = uint.Parse(fileNameOnly.Substring(0, i));
                break;
            }
            var ext = System.IO.Path.GetExtension(fileName)?.ToUpper()?.TrimStart('.');
            switch (ext)
            {
                case "JPG":
                    if (fileIndex < 0 || fileIndex > 64) throw DWINException.CreateFileIndex(fileName);
                    using (var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        GetInterface<IPictureStorage>().UploadPicture(fileIndex, f, System.Drawing.Imaging.ImageFormat.Jpeg, verify);
                    return true;
                case "BMP":
                    if (fileIndex < 0 || fileIndex > 64) throw DWINException.CreateFileIndex(fileName);
                    using (var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        GetInterface<IPictureStorage>().UploadPicture(fileIndex, f, System.Drawing.Imaging.ImageFormat.Bmp, verify);
                    return true;
                case "WAV":
                    if (fileIndex < 0 || fileIndex > 127) throw DWINException.CreateFileIndex(fileName);
                    GetInterface<IMusicStorage>().UploadWave(fileIndex, fileName);
                    return true;
                case "HZK":
                case "DZK":
                case "BIN":
                case "ICO":
                    if (fileName.StartsWith("T5", StringComparison.InvariantCultureIgnoreCase))
                        return false; //skip - unable to upload it
                    if (fileName.StartsWith("DWINOS", StringComparison.InvariantCultureIgnoreCase))
                    {
                        OS_Update(System.IO.File.ReadAllBytes(fileName), 0x10);
                    }
                    else
                    {
                        if (fileIndex < 0 || fileIndex > 127) throw DWINException.CreateFileIndex(fileName);
                        var address = (uint)fileIndex * NandAccessor.FONT_BLOCK_SIZE; //256kb blocks
                        Nand.Write(address, new ArraySegment<byte>(System.IO.File.ReadAllBytes(fileName)), verify);
                    }
                    return true;
                case "LIB":
                    if (fileIndex < 0 || fileIndex > 80) throw DWINException.CreateFileIndex(fileName);
                    Nor.Write(fileIndex * 0x1000u, new ArraySegment<byte>(System.IO.File.ReadAllBytes(fileName)), verify);
                    return true;
            }
            return false;
        }

        public virtual void Format(Action<int> progress = null)
        {
            var zeroBuffer = new byte[Core.T5.NandAccessor.NAND_WRITE_MIN_LEN]; //32KB buffer
            SRAM.Write(BufferAddress, new ArraySegment<byte>(zeroBuffer), false);
        }
    }
}
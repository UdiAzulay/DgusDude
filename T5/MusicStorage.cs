using System;

namespace DgusDude.T5
{
    public class MusicStorage : Core.MusicStorage
    {
        public readonly uint StorageOffset;
        public MusicStorage(Device device, uint length, uint storageOffset)
            :base(device, length, 0x020000 /*128kb*/)
        { StorageOffset = storageOffset; }

        public override void Play(int start, uint length, byte volume = 0xFF)
        {
            if (start + length > Length) throw DWINException.CreateOutOfRange((int)(start + length), Length);
            Device.VP.Write(0xA0, new byte[] { (byte)start, (byte)length, volume, 0x00 });
        }

        /* each section is 2.048sec at 32KHz 16bit      */
        /* (2 bytes * 32000 * 2.048 = 131072 = 0x20000) */
        public override void Upload(int index, System.IO.Stream stream, bool verify = false)
        {
            if (index > Length) throw DWINException.CreateOutOfRange(index, Length);
            var address = (int)((index * BlockSize) + StorageOffset);
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
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
            Device.Storage.Write(address, new ArraySegment<byte>(data, (int)pos, (int)chunkSize), verify);
        }
    }
}

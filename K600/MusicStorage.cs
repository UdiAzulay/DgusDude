using System;
namespace DgusDude.K600
{
    public class MusicStorage : Core.MusicStorage
    {
        public MusicStorage(Device device, uint length)
            : base(device, length, 0x020000 /*128kb*/) { }
        
        public override void Play(int start, uint length, byte volume)
        {
            if (start + length > Length) throw Exception.CreateOutOfRange((int)(start + length), Length);
            Device.VP.Write(0x50, new byte[] { 0x5A, (byte)start, (byte)length, 0x5A, volume });
        }

        public override void Upload(int index, System.IO.Stream stream, bool verify = false)
        {
            if (index > Length) throw Exception.CreateOutOfRange(index, Length);
            throw new NotImplementedException();
        }
    }
}

using System;

namespace DgusDude.Core
{
    public abstract class MusicStorage
    {
        protected readonly Device Device;
        public readonly uint BlockSize;
        public uint Length { get; private set; }
        public override string ToString() { return string.Format("Max {0} items", Length); }
        public MusicStorage(Device device, uint length, uint blockSize) { Device = device; Length = length; BlockSize = blockSize; }
        public abstract void Play(int start, uint length, byte volume = 0xFF);
        public void Stop() => Play(0, 0, 0);
        public abstract void Upload(int index, System.IO.Stream stream, bool verify = false);
    }
}

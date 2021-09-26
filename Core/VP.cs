using System;

namespace DgusDude.Core
{
    public abstract class VP : IDeviceAccessor
    {
        private const uint VP_WAIT_TIMEOUT = 1500;
        public Device Device { get; protected set; }
        protected VP(Device device) { Device = device; }
        public abstract void Read(uint address, ArraySegment<byte> data);
        public abstract void Write(uint address, ArraySegment<byte> data);

        void IDeviceAccessor.Write(uint address, ArraySegment<byte> data, bool verify) { Write(address, data); }
        public void Write(uint address, byte[] data) { Write(address, new ArraySegment<byte>(data)); }
        public byte[] Read(uint address, int len)
        {
            var ret = new byte[len];
            Read(address, new ArraySegment<byte>(ret));
            return ret;
        }

        public virtual void Wait(uint vpAddress, int checkValue = 0x5A, int checkIndex = 0, int readlength = 2)
        {
            var loopCount = VP_WAIT_TIMEOUT / 100;
            while (Read(vpAddress, readlength)[checkIndex] == checkValue && loopCount > 0)
            {
                System.Threading.Thread.Sleep(100);
                loopCount--;
            }
            if (loopCount <= 0)
                throw new DWINException(string.Format("DeviceWait indication is not clear at VP 0x{0:X4}", vpAddress));
        }

        public UserPacket ReadKey(int timeout = -1)
        {
            var readTimeout = Device.SerialPort.ReadTimeout;
            var header = new PacketHeader(Device, 0x83);
            Device.SerialPort.ReadTimeout = timeout;
            try {
                Device.RawRead(0, header.Data);
                var ret = new UserPacket(this, header.Address, header.DataLength);
                Device.RawRead(0, new ArraySegment<byte>(ret.Data));
                return ret;
            } catch (TimeoutException) {
                return null;
            } finally {
                Device.SerialPort.ReadTimeout = readTimeout;
            }
        }

    }
}

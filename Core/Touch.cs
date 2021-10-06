using System;

namespace DgusDude.Core
{
    public class Touch
    {
        protected readonly Device Device;
        public Touch(Device device) { Device = device; }

        public virtual TouchStatus Current => throw new NotImplementedException();
        public virtual UserPacket ReadKey(int timeout = -1) 
        {
            if ((Device.Platform & Platform.TouchScreen) != Platform.TouchScreen)
                throw new NotSupportedException();
            var readTimeout = Device.Connection.ReadTimeout;
            var header = new PacketHeader((Device.VP.Memory as MemoryDirectAccessor).AddressMode, Device.Config.Header.Length, 0);
            Device.Connection.ReadTimeout = timeout;
            try {
                Device.RawRead(0, header.Data);
                var ret = new UserPacket(Device.RAM, header.Address, header.DataLength);
                Device.RawRead(0, new ArraySegment<byte>(ret.Data));
                return ret;
            } catch (TimeoutException) {
                return null;
            } finally {
                Device.Connection.ReadTimeout = readTimeout;
            }
        }

        public virtual void Simulate(TouchAction action, uint x, uint y) => throw new NotImplementedException();
        public virtual void EnableControl(int pictureId, int controlId, bool value) => throw new NotImplementedException();
    }
}
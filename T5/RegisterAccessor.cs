using System;

namespace DgusDude.Core.T5
{
    public class RegisterAccessor : MemoryAccessor
    {
        public RegisterAccessor(T5Base device, uint length) : base(device, length, 1, T5Base.DWIN_REG_MAX_RW_BLEN) { }
        public override void Read(uint address, ArraySegment<byte> data)
        {
            ValidateReadAddress(address, data.Count);
            var retries = Device.Config.Retries - 1;
            var writeHeader = new PacketHeader(Device, 0x81) { DataLength = 1 }; //read regsiter command
            var readHeader = new ArraySegment<byte>(new byte[5 + writeHeader.HeaderLength]);
            foreach (var p in new Slicer(data, 0x100, address)) //per page
                foreach (var v in new Slicer(p, MaxPacketLength))
                    for (var r = 0; r <= retries; r++)
                        try
                        {
                            writeHeader.Address = (ushort)address;
                            Device.RawWrite(r, writeHeader.Data, new ArraySegment<byte>(new[] { (byte)v.Count }));
                            Device.RawRead(r, readHeader, v);
                            address += (ushort)v.Count;
                            break;
                        }
                        catch (TimeoutException)
                        {
                            if (r == retries) throw DWINException.CreateTimeout(writeHeader.Command, address);
                            System.Threading.Thread.Sleep(Device.Config.RetryWait);
                        }

        }

        public override void Write(uint address, ArraySegment<byte> data, bool verify = false)
        {
            ValidateWriteAddress(address, data.Count);
            var retries = Device.Config.Retries - 1;
            var writeHeader = new PacketHeader(Device, 0x80); //write regsiter command
            foreach (var p in new Slicer(data, 0x100, address)) //per page
                foreach (var v in new Slicer(p, MaxPacketLength))
                    for (var r = 0; r <= retries; r++)
                        try
                        {
                            writeHeader.Address = (ushort)address;
                            writeHeader.DataLength = (byte)v.Count;
                            Device.RawWrite(r, writeHeader.Data, v);
                            Device.RawRead(r, new ArraySegment<byte>(new byte[4 + writeHeader.HeaderLength])); //expect ack
                            if (verify) Verify(address, v);
                            address += (ushort)v.Count;
                            break;
                        }
                        catch (DWINVerifyException)
                        {
                            if (r == retries) throw;
                        }
                        catch (TimeoutException)
                        {
                            if (r == retries) throw DWINException.CreateTimeout(writeHeader.Command, address);
                            System.Threading.Thread.Sleep(Device.Config.RetryWait);
                        }
        }
    }
}

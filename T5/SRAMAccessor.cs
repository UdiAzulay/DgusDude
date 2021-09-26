using System;
using System.Collections.Generic;
using System.Text;

namespace DgusDude.Core.T5
{
    public class SRAMAccessor : MemoryAccessor
    {
        public SRAMAccessor(T5Base device, uint length) : base(device, length, 2, T5Base.DWIN_REG_MAX_RW_BLEN) { }
        public override void Read(uint address, ArraySegment<byte> data)
        {
            ValidateReadAddress(address, data.Count);
            var retries = Device.Config.Retries - 1;
            var writeHeader = new PacketHeader(Device, 0x83) { DataLength = 1 }; //read SRAM command
            var readHeader = new ArraySegment<byte>(new byte[5 + writeHeader.HeaderLength]);
            foreach (var v in new Slicer(data, MaxPacketLength))
                for (var r = 0; r <= retries; r++)
                    try
                    {
                        writeHeader.Address = (ushort)(address >> 1);
                        Device.RawWrite(r, writeHeader.Data, new ArraySegment<byte>(new[] { (byte)(v.Count >> 1) }));
                        Device.RawRead(r, readHeader, v);
                        address += (uint)v.Count;
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
            var isOdd = (data.Count % 2) != 0;
            ValidateWriteAddress(address, data.Count + (isOdd ? 1 : 0));
            var retries = Device.Config.Retries - 1;
            var writeHeader = new PacketHeader(Device, 0x82); //write SRAM command
            foreach (var v in new Slicer(data, MaxPacketLength))
                for (var r = 0; r <= retries; r++)
                    try
                    {
                        var padByte = (isOdd && (v.Count % 2) != 0) ? 1 : 0;
                        //System.Diagnostics.Debug.Assert((v.Count % 2) == 0, "SRAM length not even");
                        System.Diagnostics.Debug.Assert((address % 2) == 0, "SRAM address not even");
                        writeHeader.Address = (ushort)(address >> 1);
                        writeHeader.DataLength = (byte)(v.Count + padByte);
                        if (padByte == 0) Device.RawWrite(r, writeHeader.Data, v);
                        else Device.RawWrite(r, writeHeader.Data, v, new ArraySegment<byte>(WritePaddingByte));
                        if (Device.Config.EnableAck) Device.RawRead(r, new ArraySegment<byte>(new byte[2 + writeHeader.HeaderLength])); //expect ack
                        if (verify) Verify(address, v);
                        address += (uint)v.Count;
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

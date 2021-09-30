using System;
namespace DgusDude
{
    [Flags]
    public enum ConnectionOptions : byte
    {
        CRC = 0x01, NoAckRAM = 0x80, NoAckREG = 0x40,
    }

    public class ConnectionConfig
    {
        public ConnectionOptions Options { get; set; } = ConnectionOptions.NoAckRAM;
        public byte[] Header { get; set; } = { 0x5A, 0xA5 };
        public byte[] AckValue { get; set; }
        public byte Retries { get; set; } = 10;
        public int RetryWait { get; set; } = 200;
        public override string ToString()
        {
            return string.Format("Header: {0}, Retries: {1}, Options: {2}",
                BitConverter.ToString(Header), Retries, Options);
        }
    }
}
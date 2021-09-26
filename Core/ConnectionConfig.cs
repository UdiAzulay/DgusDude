using System;
using System.Collections.Generic;
using System.Text;

namespace DgusDude.Core
{
    public class ConnectionConfig
    {
        public byte[] Header = { 0x5A, 0xA5 };
        public bool EnableAck { get; set; }
        public bool EnableCRC { get; set; }
        public byte Retries { get; set; } = 10;
        public int RetryWait { get; set; } = 200;
    }
}

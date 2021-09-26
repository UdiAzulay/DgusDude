using System;

namespace DgusDude.Core
{
    public class DataEventArgs
    {
        public readonly bool Write;
        public readonly ArraySegment<byte> Bytes;
        public readonly int Offset;
        public readonly int Length;
        public readonly int Retry;
        public Exception Exception;

        public DataEventArgs(bool write, ArraySegment<byte> bytes, int offset, int length, int retry, Exception exception = null)
        {
            Write = write; Bytes = bytes;
            Offset = offset; Length = length;
            Retry = retry; Exception = exception;
        }
    }
}

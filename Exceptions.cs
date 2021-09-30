using System;

namespace DgusDude
{
    public class Exception : System.Exception
    {
        public Exception(string message, System.Exception inner = null)
            : base(message, inner)
        { }
        public static Exception CreateTimeout(byte cmd, int address)
        {
            return new Exception(string.Format("Command 0x{0:X2} error reading location 0x{1:X}, operation timeout", cmd, address));
        }
        public static Exception CreateOutOfRange(Core.MemoryAccessor mem, int address)
        {
            return new Exception(string.Format("{0} Address 0x{1:X4} is out of range, range is 0-0x{2:X8}", mem.ToString(), address, mem.Length));
        }
        public static Exception CreateMemBaundary(Core.MemoryAccessor mem, int boundary)
        {
            return new Exception(string.Format("{0} Memory access must be 0x{1:X} baundary", mem.ToString(), boundary));
        }
        public static Exception CreateFileIndex(string fileName)
        {
            return new Exception(string.Format("Upload invalid file index {0}", fileName));
        }
        public static Exception CreateOutOfRange(int index, uint length)
        {
            return new Exception(string.Format("index {0} is out of range ({1} max)", index, length));
        }
        public static Exception CreateInterfaceNotExist(Type interfaceType)
        {
            return new Exception(string.Format("interface {0} not exist on device", interfaceType.Name));
        }
    }

    public class VerifyException : Exception
    {
        public VerifyException(string message, System.Exception inner = null)
            : base(message, inner)
        { }
        public static VerifyException CreateValidate(Core.MemoryAccessor mem, int address, string command)
        {
            return new VerifyException(string.Format("Wrong IO reply while {0} address 0x{1:X}", mem.ToString(), address, command));
        }
    }
}

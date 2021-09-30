using System;

namespace DgusDude
{
    public class DWINException : Exception
    {
        public DWINException(string message, Exception inner = null)
            : base(message, inner)
        { }
        public static DWINException CreateTimeout(byte cmd, int address)
        {
            return new DWINException(string.Format("Command 0x{0:X2} error reading location 0x{1:X}, operation timeout", cmd, address));
        }
        public static DWINException CreateOutOfRange(Core.MemoryAccessor mem, int address)
        {
            return new DWINException(string.Format("{0} Address 0x{1:X4} is out of range, range is 0-0x{2:X8}", mem.ToString(), address, mem.Length));
        }
        public static DWINException CreateMemBaundary(Core.MemoryAccessor mem, int boundary)
        {
            return new DWINException(string.Format("{0} Memory access must be 0x{1:X} baundary", mem.ToString(), boundary));
        }
        public static DWINException CreateFileIndex(string fileName)
        {
            return new DWINException(string.Format("Upload invalid file index {0}", fileName));
        }
        public static DWINException CreateOutOfRange(int index, uint length)
        {
            return new DWINException(string.Format("index {0} is out of range ({1} max)", index, length));
        }
        public static DWINException CreateInterfaceNotExist(Type interfaceType)
        {
            return new DWINException(string.Format("interface {0} not exist on device", interfaceType.Name));
        }
    }

    public class DWINVerifyException : DWINException
    {
        public DWINVerifyException(string message, Exception inner = null)
            : base(message, inner)
        { }
        public static DWINVerifyException CreateValidate(Core.MemoryAccessor mem, int address, string command)
        {
            return new DWINVerifyException(string.Format("Wrong IO reply while {0} address 0x{1:X}", mem.ToString(), address, command));
        }
    }
}

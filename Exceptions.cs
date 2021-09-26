using System;

namespace DgusDude
{
    public class DWINException : Exception
    {
        public DWINException(string message, Exception inner = null)
            : base(message, inner)
        { }
        public static DWINException CreateTimeout(byte cmd, uint address)
        {
            return new DWINException(string.Format("Command 0x{0:X2} error reading location 0x{1:X}, operation timeout", cmd, address));
        }
        public static DWINException CreateOutOfRange(Core.MemoryAccessor mem, uint address)
        {
            return new DWINException(string.Format("{0} Address 0x{1:X4} is out of range, range is 0-0x{2:X8}", mem.ToString(), address, mem.Length));
        }
        public static DWINException CreateMemBaundary(Core.MemoryAccessor mem, int boundary)
        {
            return new DWINException(string.Format("{0} Memory access must be 0x{1:X} baundary", mem.ToString(), boundary));
        }
        public static DWINException CreateFileIndex(string fileName)
        {
            return new DWINException(string.Format("Upload invalid file index", fileName));
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
    }
}

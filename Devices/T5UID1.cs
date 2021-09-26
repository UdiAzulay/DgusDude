using System;
using System.Linq;

namespace DgusDude.Devices
{
    public class T5UID1 : T5UIDx
    {
        public const uint MEM_WAV_BEGIN = 0x02000000; //last 32 MB

        public T5UID1() : this(null) { }
        public T5UID1(Core.ConnectionConfig config) : base(config)
        {
            //"readable area is 0x40:000000-0x80:020000 (16MB of the 64MB)"
            //64MB 0x00000000:04000000
            //16MB 0x00000000:00FFFFFF - system fonts - non readable
            //16MB 0x01000000:01FFFFFF - user data - readable
            //32MB 0x02000000:03FFFFFF - audio - non readable
            Nand = new Core.T5.NandAccessor(this, 0x04000000); //64MB
            Pictures = new PictureStorage(this, 64);
            Music = new MusicStorage(Nand, 64, MEM_WAV_BEGIN);
            PWM = new T5UID1PWM(this);
            ADC = new T5UID1ADC(this);
            DeviceInfo = new UID1DeviceInfo(this);
        }

        public override T GetInterface<T>()
        {
            if (typeof(T) == typeof(IPWM)) return PWM as T;
            else if (typeof(T) == typeof(IADC)) return ADC as T;
            else if (typeof(T) == typeof(IDeviceInfo)) return DeviceInfo as T;
            return base.GetInterface<T>();
        }

        public override void Format(Action<int> progress = null)
        {
            var zeroBuffer = new byte[Core.T5.NandAccessor.NAND_WRITE_MIN_LEN]; //32KB buffer
            SRAM.Write(BufferAddress, new ArraySegment<byte>(zeroBuffer), false);
            //font memory 64MB
            progress?.Invoke(10);
            var bufferAddress = (BufferAddress >> 1).ToLittleEndien(2);
            for (ushort i = 0; i < 0x0800; i++)
            {
                Nand.Write(i * (uint)zeroBuffer.Length, BufferAddress, zeroBuffer.Length);
                progress?.Invoke(10 + (i * 45 / 0x07FF));
            }

            for (uint i = 0; i < 0x4B000; i += (uint)zeroBuffer.Length)
            {
                var dataLength = ((uint)Math.Min(0x4B000 - i, zeroBuffer.Length) >> 1).ToLittleEndien(2);
                var imagePosition = (i >> 1).ToLittleEndien(3);
                VP.Write(0xA2, new byte[] {
                    0x5A, //fixed
                    bufferAddress[0], bufferAddress[1], //SRAM position
                    dataLength[0], dataLength[1], //data length in words
                    imagePosition[0], imagePosition[1], imagePosition[2] //image buffer position
                });
            }

            var ps = GetInterface<IPictureStorage>();
            GetInterface<IDrawPicture>().Clear();
            for (uint i = 0; i < ps.Length; i++)
            {
                ps.TakeScreenshot(i);
                progress?.Invoke(55 + ((int)i * 45 / 0xFF));
            }
        }

        protected class UID1DeviceInfo : T5DeviceInfo
        {
            public UID1DeviceInfo(T5UID1 device) : base(device) { }
            public override float CpuTemprature => Device.VP.Read(0x37, 2).FromLittleEndien(0, 2) / 10f;
        }
        public Core.T5.DeviceInfo DeviceInfo { get; private set; }

        public class T5UID1PWM : IPWM, System.Collections.ICollection, System.Collections.Generic.IReadOnlyCollection<ushort>
        {
            public readonly Device Device;
            public T5UID1PWM(Device device) { Device = device; }
            public int Count { get; private set; } = 3;
            public ushort this[int index] { get { return Read(index, 1)[0]; } set { } }
            public void SetPWM(byte index, byte div, UInt16 acuracy)
            {
                if (index > Count) throw new Exception("PWM indexes are 0-2");
                var accuracyBytes = ((uint)acuracy).ToLittleEndien(2);
                Device.VP.Write(0x86 + (uint)(index * 4), new byte[] {
                    0x5A, // fixed
                    div, //division factor
                    accuracyBytes[0], accuracyBytes[1], //accuracy
                    0x00, 0x00, 0x00, 0x00 //fixed
                });
            }
            public ushort[] Read(int startIndex, int length)
            {
                if ((startIndex + length) > Count) throw new DWINException("PWM indexes are 0-" + Count);
                var ret = Device.VP.Read(0x92 + (uint)startIndex, 2 * length);
                return Enumerable.Range(0, length)
                    .Select(v => (ushort)ret.FromLittleEndien(v * 2, 2)).ToArray();
            }
            
            bool System.Collections.ICollection.IsSynchronized => false;
            object System.Collections.ICollection.SyncRoot => null;
            void System.Collections.ICollection.CopyTo(Array array, int index) {
                if (array.Length - index < Count) throw new IndexOutOfRangeException();
                Read(0, Count).CopyTo(array, index);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return Read(0, Count).GetEnumerator();
            }
            System.Collections.Generic.IEnumerator<ushort> System.Collections.Generic.IEnumerable<ushort>.GetEnumerator()
            {
                return Read(0, Count).GetEnumerator() as System.Collections.Generic.IEnumerator<ushort>;
            }
        }
        public T5UID1PWM PWM { get; private set; }

        public class T5UID1ADC : IADC, System.Collections.ICollection, System.Collections.Generic.IReadOnlyCollection<Tuple<ushort, ushort>>
        {
            public readonly Device Device;
            public int Count { get; private set; } = 7;
            public T5UID1ADC(Device device) { Device = device; }

            public Tuple<ushort, ushort> this[int index] { get => Read(index, 1)[0]; }

            public Tuple<ushort, ushort>[] Read(int startIndex, int length)
            {
                var ret = Device.VP.Read(0x38 + (uint)(startIndex * 4), length * 4);
                return Enumerable.Range(0, ret.Length / 4)
                    .Select(i => new Tuple<ushort, ushort>((ushort)ret.FromLittleEndien(i, 2), (ushort)ret.FromLittleEndien(i + 2, 2))).ToArray();
            }
            bool System.Collections.ICollection.IsSynchronized => false;
            object System.Collections.ICollection.SyncRoot => null;
            void System.Collections.ICollection.CopyTo(Array array, int index)
            {
                if (array.Length - index < Count) throw new IndexOutOfRangeException();
                Read(0, Count).CopyTo(array, index);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return Read(0, Count).GetEnumerator();
            }
            System.Collections.Generic.IEnumerator<Tuple<ushort, ushort>> System.Collections.Generic.IEnumerable<Tuple<ushort, ushort>>.GetEnumerator()
            {
                return Read(0, Count).GetEnumerator() as System.Collections.Generic.IEnumerator<Tuple<ushort, ushort>>;
            }
        }
        public T5UID1ADC ADC { get; private set; }
    }
}

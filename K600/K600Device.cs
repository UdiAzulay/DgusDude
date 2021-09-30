using System;
namespace DgusDude.K600
{
    using Core;
    public class K600Device : Device
    {
        public K600Device(Platform platform, LCD screen, uint? flashSize) : base(platform, screen)
        {
            Registers = new MemoryDirectAccessor(this, 0x100, 1, AddressMode.Size1, 0x81, 0x80); //128 Registers
            RAM = new MemoryDirectAccessor(this, 0x7000, 2, AddressMode.Size2 | AddressMode.Shift1 | AddressMode.ShiftRead, 0x83, 0x82); //128kb

            Buffer = new MemoryBuffer(RAM, 0x10000, 0x10000);
            VP = new VP(Registers);

            if (!flashSize.HasValue) flashSize = 0x40000;
            Storage = new NandAccessor(this, flashSize.Value, 2, 0x40000);
            
            Pictures = new PictureStorage(this, (flashSize.Value >> 1) / screen.FrameSize);
            Music = new MusicStorage(this, (flashSize.Value >> 1) / 0x20000 /*128k*/);
        }

        public override Tuple<byte, byte> Version => new Tuple<byte, byte>(VP.Read(0x00, 1)[0], 0);
        public override void Reset(bool cpuOnly) => VP.Write(0xEE, new byte[] { 0x5A, 0xA5 });
        public override DateTime Time
        {
            get
            {
                var ret = VP.Read(0x20, 16);
                if (ret[1] == 0 || ret[2] == 0) return DateTime.MinValue;
                return new DateTime(2000 + ret[0].FromBCD(), ret[1].FromBCD(), ret[2].FromBCD(), ret[4].FromBCD(), ret[5].FromBCD(), ret[6].FromBCD());
            }
            set
            {
                var hasRTC = (Platform & Platform.RTC) != 0;
                VP.Write(0x20, new byte[] {
                    (byte)(hasRTC ? 0x5A : 0x00),
                    (value.Year % 100).ToBCD(), value.Month.ToBCD(), value.Day.ToBCD(), ((int)value.DayOfWeek).ToBCD(),
                    value.Hour.ToBCD(), value.Minute.ToBCD(), value.Second.ToBCD(), 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                });
            }
        }

        public byte Brightness { get { return VP.Read(0x01, 1)[0]; } set { VP.Write(0x01, new[] { value }); } }
        public override TouchStatus GetTouch()
        {
            if ((Platform & Platform.TouchScreen) == 0) return base.GetTouch();
            return new TouchStatus(this, 0x05, 7);
        }

        public override void Format(Action<int> progress = null) {
            throw new NotImplementedException();
        }

        protected override void UploadBin(int libIndex, byte[] data, bool verify = false) {
            throw new NotImplementedException();
        }
    }
}

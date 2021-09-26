using System;

namespace DgusDude
{
    public interface IUpload
    {
        void Format(Action<int> progress = null);
        bool Upload(string fileName, bool verify = false);
    }

    public interface IRTC
    {
        bool HasHardwareRTC { get; }
        DateTime Time { get; set; }
    }

    public interface IBacklightControl
    {
        byte Normal { get; set; }
        byte StandBy { get; set; }
        uint StandByTime { get; set; }
    }

    public interface IPWM
    {
        int Count { get; }
        ushort this[int index] { get; set; }
        ushort[] Read(int startIndex, int length);
    }

    public interface IADC
    {
        int Count { get; }
        Tuple<ushort, ushort> this[int index] { get; }
        Tuple<ushort, ushort>[] Read(int startIndex, int length);
    }

    public enum PictureUploadMode { Jpeg, Bitmap, BitmapSwapBytes }
    public interface IPictureStorage
    {
        uint Length { get; }
        uint Current { get; set; }
        void UploadPicture(uint pictureId, System.IO.Stream stream, System.Drawing.Imaging.ImageFormat format, bool verify);
        void TakeScreenshot(uint pictureId);
    }

    public interface IDrawPicture
    {
        void Draw(byte[] pixelData, System.Drawing.Point topLeft, System.Drawing.Imaging.ImageFormat format);
        void Clear();
        //void Draw(System.Drawing.Image image, System.Drawing.Point srcRect, System.Drawing.Point topLeft);
    }

    public interface IMusicStorage
    {
        uint Length { get; }
        void Play(int start, int length, byte volume);
        void Stop();
        void UploadWave(uint waveId, byte[] samples);
        void UploadWave(uint waveId, string fileName);
    }

    public enum TouchAction : byte { Release = 0, Press = 1, Lift = 2, Pressing = 3 }
    public interface ITouchStatus
    {
        TouchAction Status { get; }
        uint X { get; }
        uint Y { get; }
        void Simulate(TouchAction action, uint x, uint y);
    }

    public interface IDeviceInfo
    {
        bool IsIdle { get; }
        float Vcc { get; }
        float CpuTemprature { get; }
        Tuple<byte, byte> Version { get; }
        ulong DeviceID { get; }
        string SDUploadFolder { get; }
    }
}
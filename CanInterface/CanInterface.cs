using System;

namespace CanInterface
{
    public enum Baudrate
    {
        Baudrate1000000,
        Baudrate800000,
        Baudrate500000,
        Baudrate400000,
        Baudrate250000,
        Baudrate200000,
        Baudrate125000,
        Baudrate100000,
        Baudrate50000,
        Baudrate20000,
        Baudrate10000,
        Baudrate5000,
    };

    public enum FrameType
    {
        Standard,
        Extended,
        Error
    };

    public class Frame
    {
        public int Id;
        public byte[] Data = new byte[8];
        public FrameType Type = FrameType.Standard;
    }

    public interface ICanInterface
    {
        void Open(Baudrate speed);
        void SendFrame(Frame frame);
        Frame ReceiveFrame(bool blocking = true);
        void Close();
    }
}

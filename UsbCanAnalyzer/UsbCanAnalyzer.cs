using System;
using System.IO.Ports;
using System.Threading;
using CanInterface;

namespace UsbCanAnalyzer
{
    public enum UsbSpeed
    {
        Speed_1000000 = 0x01,
        Speed_800000 = 0x02,
        Speed_500000 = 0x03,
        Speed_400000 = 0x04,
        Speed_250000 = 0x05,
        Speed_200000 = 0x06,
        Speed_125000 = 0x07,
        Speed_100000 = 0x08,
        Speed_50000 = 0x09,
        Speed_20000 = 0x0a,
        Speed_10000 = 0x0b,
        Speed_5000 = 0x0c,
    };

    public enum UsbFrameType
    {
        Standard = 0x01,
        Extended = 0x02,
    };

    public enum Mode
    {
        Normal = 0x00,
        Loopback = 0x01,
        Silent = 0x02,
        LoopbackSilent = 0x03,
    };

    public class UsbCan : ICanInterface
    {
        SerialPort Port;
        public UsbCan(string portname)
        {
            Port = new SerialPort(portname, 2000000);
        }

        public void Open(Baudrate speed)
        {
            Port.Open();
            UsbSpeed br;
            switch(speed)
            {
                case Baudrate.Baudrate10000:
                    br = UsbSpeed.Speed_10000;
                    break;
                case Baudrate.Baudrate20000:
                    br = UsbSpeed.Speed_20000;
                    break;
                case Baudrate.Baudrate50000:
                    br = UsbSpeed.Speed_50000;
                    break;
                case Baudrate.Baudrate100000:
                    br = UsbSpeed.Speed_100000;
                    break;
                case Baudrate.Baudrate200000:
                    br = UsbSpeed.Speed_200000;
                    break;
                case Baudrate.Baudrate400000:
                    br = UsbSpeed.Speed_400000;
                    break;
                case Baudrate.Baudrate500000:
                    br = UsbSpeed.Speed_500000;
                    break;
                case Baudrate.Baudrate1000000:
                    br = UsbSpeed.Speed_1000000;
                    break;
                default:
                    throw new Exception("Invalid baudrate.");
            }
            var settings = Settings(br, Mode.Normal, UsbFrameType.Standard);
            Port.Write(settings, 0, settings.Length);
            Port.ReadExisting();
            Thread.Sleep(100);
        }

        public void Close()
        {
            Port.Close();
        }
        static byte Checksum(byte[] data, int start, int data_len)
        {
            int i, checksum;

            checksum = 0;
            for (i = 0; i < data_len; i++)
            {
                checksum += data[start + i];
            }

            return (byte)(checksum & 0xff);
        }
        static byte[] Settings(UsbSpeed speed, Mode mode, UsbFrameType frame)
        {
            var bytes = new byte[] {
                0xaa,
                0x55,
                0x12,
                (byte)speed,
                (byte)frame,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                (byte)mode,
                0x01,
                0,
                0,
                0,
                0,
                0
            };
            bytes[19] = Checksum(bytes, 2, 17);
            return bytes;
        }

        static bool FrameComplete(byte[] buffer, int len)
        {
            if (len < 2)
                return false;

            if (buffer[1] == 0xaa)
            {
                return len >= 20;
            }
            else if ((buffer[1] >> 4) == 0xc)
            {
                return len >= (buffer[1] & 0xf) + 5;
            }

            return true;
        }

        public void SendFrame(Frame frame)
        {
            byte[] data = new byte[13];
            int len = 0;

            data[len++] = 0xaa;
            data[len] = 0xC0;
            if (frame.Type == FrameType.Standard)
            {
                data[len] &= 0xDF;
            }
            else
            {
                data[len] |= 0x20;
            }
            data[len] &= 0xEF;
            data[len] |= (byte)frame.Data.Length;
            len++;


            data[len++] = (byte)(frame.Id & 0xff);
            data[len++] = (byte)((frame.Id >> 8) & 0xff);

            for (int i = 0; i < frame.Data.Length; ++i)
            {
                data[len++] = frame.Data[i];
            }
            data[len++] = 0x55;

            Port.Write(data, 0, len);
        }

        byte[] buffer = new byte[100];
        int pos = 0;
        bool start_found = false;

        public Frame ReceiveFrame(bool blocking = true)
        {
            while (true)
            {
                if((Port.BytesToRead == 0) && (!blocking))
                {
                    return null;
                }
                var newbyte = Port.ReadByte();
                if (!start_found)
                {
                    start_found = (newbyte == 0xaa);
                }
                if (start_found)
                {
                    buffer[pos++] = (byte)newbyte;
                }

                if (FrameComplete(buffer, pos))
                {
                    if ((pos == 20) && (buffer[0] == 0xaa) && (buffer[1] == 0x55))
                    {
                        byte checksum = Checksum(buffer, 2, 17);
                        if (checksum != buffer[pos - 1])
                        {
                            start_found = false;
                            pos = 0;
                            continue;
                        }
                    }

                    if ((pos >= 6) &&
                      (buffer[0] == 0xaa) &&
                      ((buffer[1] >> 4) == 0xc))
                    {
                        var frame = new Frame();
                        //Console.Write("Frame ID: {0:X2}{1:X2}, Data: ", buffer[3], buffer[2]);
                        frame.Id = (buffer[3] << 8) & buffer[2];
                        int j = 0;
                        for (int i = 4; i <= pos - 2; i++)
                        {
                            frame.Data[j++] = buffer[i];
                            //Console.Write("{0:X2} ", buffer[i]);
                        }
                        //Console.WriteLine();
                        start_found = false;
                        pos = 0;
                        return frame;
                    }

                    start_found = false;
                    pos = 0;
                }
            }
        }
    }
}

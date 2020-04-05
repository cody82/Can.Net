using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CanInterface;
using Peak.Can.Basic;

namespace PeakCan
{
    public class PCanException : Exception
    {
        TPCANStatus Error;

        public PCanException(string message, TPCANStatus error) : base(message)
        {
            Error = error;
        }
        public PCanException(TPCANStatus error) : base(error.ToString())
        {
            Error = error;
        }
        public PCanException(string message) : base(message)
        {
            Error = TPCANStatus.PCAN_ERROR_OK;
        }
    }

    public class PCanDevice : ICanInterface
    {
        ushort Channel;
        public PCanDevice(ushort channel = PCANBasic.PCAN_USBBUS1)
        {
            Channel = channel;
        }

        public void Close()
        {
            var ret = PCANBasic.Uninitialize(Channel);
            CheckError(ret);
        }

        public void Open(Baudrate speed)
        {
            TPCANBaudrate bitrate;

            switch(speed)
            {
                case Baudrate.Baudrate5000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_5K;
                    break;
                case Baudrate.Baudrate10000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_10K;
                    break;
                case Baudrate.Baudrate20000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_20K;
                    break;
                case Baudrate.Baudrate50000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_50K;
                    break;
                case Baudrate.Baudrate100000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_100K;
                    break;
                case Baudrate.Baudrate125000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_125K;
                    break;
                case Baudrate.Baudrate250000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_250K;
                    break;
                case Baudrate.Baudrate500000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_500K;
                    break;
                case Baudrate.Baudrate800000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_800K;
                    break;
                case Baudrate.Baudrate1000000:
                    bitrate = TPCANBaudrate.PCAN_BAUD_1M;
                    break;
                default:
                    throw new Exception("Invalid baudrate.");
            }

            var ret = PCANBasic.Initialize(Channel, bitrate);
            CheckError(ret);
        }

        void CheckError(TPCANStatus status)
        {
            if(status != TPCANStatus.PCAN_ERROR_OK)
            {
                throw new PCanException(status);
            }
        }

        public Frame ReceiveFrame(bool blocking = true)
        {
            TPCANStatus ret;
            do
            {
                ret = PCANBasic.Read(Channel, out TPCANMsg msg, out TPCANTimestamp time);
                if (ret == TPCANStatus.PCAN_ERROR_OK)
                {
                    var frame = new Frame()
                    {
                        Data = msg.DATA,
                        Id = (int)msg.ID,
                    };

                    if (msg.MSGTYPE == TPCANMessageType.PCAN_MESSAGE_STANDARD)
                    {
                        frame.Type = FrameType.Standard;
                    }
                    else if (msg.MSGTYPE == TPCANMessageType.PCAN_MESSAGE_EXTENDED)
                    {
                        frame.Type = FrameType.Extended;
                    }
                    else if (msg.MSGTYPE == TPCANMessageType.PCAN_MESSAGE_EXTENDED)
                    {
                        frame.Type = FrameType.Error;
                    }
                    else
                    {
                        return null;
                    }

                    return frame;
                }
                else if (ret == TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                {
                    if(!blocking)
                        return null;
                }
                else
                {
                    CheckError(ret);
                }
                Thread.Sleep(1);
            } while (blocking && ret == TPCANStatus.PCAN_ERROR_QRCVEMPTY);
            return null;
        }

        public void SendFrame(Frame frame)
        {
            var msg = new TPCANMsg()
            {
                DATA = frame.Data,
                ID = (uint)frame.Id,
                LEN = (byte)frame.Data.Length,
                MSGTYPE = frame.Type == FrameType.Standard ? TPCANMessageType.PCAN_MESSAGE_STANDARD : TPCANMessageType.PCAN_MESSAGE_EXTENDED
            };
            var ret = PCANBasic.Write(Channel, ref msg);
            CheckError(ret);
        }
    }
}

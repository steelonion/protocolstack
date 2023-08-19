using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Net;
using System.Threading;

namespace SteelOnion.ProtocolStack.Protocol.TCP
{
    /// <summary>
    /// TCP状态机
    /// </summary>
    internal class TcpStateController
    {
        private static Random Random = new Random();

        /// <summary>
        /// 数据缓冲区长度
        /// </summary>
        private static int BufferLength = 65535;

        internal SimulatedTcpClient TcpClient { get; }

        internal EventWaitHandle WaitHandle { get; private set; }

        /// <summary>
        /// 序列号
        /// </summary>
        internal uint Seq { get; private set; }
        /// <summary>
        /// 确认号
        /// </summary>
        internal uint Ack { get; private set; }
        /// <summary>
        /// 数据缓冲区
        /// </summary>
        private byte[] _buffer = new byte[65535];

        /// <summary>
        /// 缓冲区队尾位置
        /// </summary>
        private int _bufferPosition;

        internal ushort LocalPort { get; }
        internal ushort RemotePort { get; }
        private IPAddress RemoteAddress { get; }

        /// <summary>
        /// TCP连接的当前状态
        /// </summary>
        public ETcpState TcpState { get; set; }

        public TcpStateController(EventHandler<ProtocolIPArgs> send, ushort localPort, IPEndPoint remote)
        {
            _bufferPosition = 0;
            _buffer = new byte[BufferLength];
            Seq = (uint)Random.Next();
            LocalPort = localPort;
            RemotePort = (ushort)remote.Port;
            RemoteAddress = remote.Address;
            TcpPacketSend = send;
            //新连接总是在关闭状态
            TcpState = ETcpState.CLOSED;
            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public event EventHandler<ProtocolIPArgs> TcpPacketSend;

        public void ReceiveTcpPacket(TcpPacket packet)
        {
            switch (TcpState)
            {
                case ETcpState.SYN_SENTY://客户端等待响应
                    {
                        if (packet.Acknowledgment && packet.Synchronize)
                        {
                            //SYN响应包
                            //发送应答
                            var retPacket = new TcpPacket(LocalPort, RemotePort);
                            retPacket.Acknowledgment = true;
                            Ack = packet.SequenceNumber + 1;
                            retPacket.AcknowledgmentNumber = Ack;
                            Seq += 1;
                            retPacket.SequenceNumber = Seq;
                            retPacket.WindowSize = 65535;
                            TcpPacketSend?.Invoke(this, new ProtocolIPArgs(retPacket, RemoteAddress, null));
                            //客户端进入可通讯
                            TcpState = ETcpState.ESTABLISHED;
                            //停止阻塞
                            WaitHandle.Set();
                        }
                    }
                    break;

                case ETcpState.ESTABLISHED://正常通讯
                    {
                        if (packet.Acknowledgment && packet.Finished)
                        {
                            TcpState = ETcpState.CLOSE_WAIT;
                            ReturnAcknowledgmentPacket(packet);
                        }
                        if (packet.Acknowledgment)
                        {
                            if (packet.PayloadData.Length + _bufferPosition < BufferLength)
                            {
                                packet.PayloadData.CopyTo(_buffer, _bufferPosition);
                                _bufferPosition += packet.PayloadData.Length;
                                ReturnAcknowledgmentPacket(packet);
                            }
                            if (packet.Push)
                            {
                                //把缓冲区的数据压入客户端
                                _bufferPosition = 0;
                            }
                        }
                    }
                    break;

                case ETcpState.FIN_WAIT_1:
                    {
                        if (packet.Acknowledgment)
                        {
                            //收到确认包
                            TcpState = ETcpState.FIN_WAIT_2;
                        }
                    }
                    break;
                case ETcpState.FIN_WAIT_2:
                    {
                        if (packet.Acknowledgment && packet.Finished)
                        {
                            ReturnAcknowledgmentPacket(packet);
                            TcpState = ETcpState.CLOSED;
                        }
                    }
                    break;
                case ETcpState.CLOSE_WAIT:
                    {
                        if (packet.Acknowledgment)
                        {
                            //收到最终的ACK，连接关闭
                            TcpState = ETcpState.CLOSED;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 发送响应
        /// </summary>
        /// <param name="packet">源包</param>
        private void ReturnAcknowledgmentPacket(TcpPacket packet)
        {
            if (packet.Acknowledgment)
            {
                if (packet.Synchronize)
                {
                    //连接建立响应
                    var retPacket = new TcpPacket(LocalPort, RemotePort);
                    retPacket.Acknowledgment = true;
                    Ack = packet.SequenceNumber + 1;
                    retPacket.AcknowledgmentNumber = Ack;
                    Seq += 1;
                    retPacket.SequenceNumber = Seq;
                    retPacket.WindowSize = 65535;
                    TcpPacketSend?.Invoke(this, new ProtocolIPArgs(retPacket, RemoteAddress, null));
                }
                else if (packet.Finished)
                {
                    //连接断开响应
                    var retPacket = new TcpPacket(LocalPort, RemotePort);
                    retPacket.Acknowledgment = true;
                    Ack = packet.SequenceNumber + 1;
                    retPacket.AcknowledgmentNumber = Ack;
                    retPacket.Finished = true;
                    Seq += 1;
                    retPacket.SequenceNumber = Seq;
                    retPacket.WindowSize = 65535;
                    TcpPacketSend?.Invoke(this, new ProtocolIPArgs(retPacket, RemoteAddress, null));
                }
                else
                {
                    //空包不做响应
                    if (packet.PayloadData.Length > 0)
                    {
                        //正常数据响应
                        var retPacket = new TcpPacket(LocalPort, RemotePort);
                        retPacket.Acknowledgment = true;
                        Ack = (uint)(packet.SequenceNumber + packet.PayloadData.Length);
                        retPacket.AcknowledgmentNumber = Ack;
                        retPacket.WindowSize = 65535;
                        retPacket.SequenceNumber = Seq;
                        retPacket.Acknowledgment = true;
                        retPacket.AcknowledgmentNumber = packet.SequenceNumber + (uint)packet.PayloadData.Length;
                        TcpPacketSend?.Invoke(this, new ProtocolIPArgs(retPacket, RemoteAddress, null));
                    }
                }
            }
        }

        public void Disconnect()
        {
            if (TcpState == ETcpState.ESTABLISHED)
            {
                //开始四次挥手
                TcpPacket packet = new TcpPacket(LocalPort, RemotePort);
                packet.SequenceNumber = Seq;
                packet.Synchronize = true;
                Ack = packet.SequenceNumber + 1;
                packet.AcknowledgmentNumber = Ack;
                packet.WindowSize = 65535;
                TcpPacketSend?.Invoke(this, new ProtocolIPArgs(packet, RemoteAddress, null));
                TcpState = ETcpState.FIN_WAIT_1;
                WaitHandle.Reset();
                WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            }
        }

        public bool Connect()
        {
            if (TcpState == ETcpState.CLOSED)
            {
                //开始三次握手
                TcpPacket packet = new TcpPacket(LocalPort, RemotePort);
                packet.SequenceNumber = Seq;
                packet.Synchronize = true;
                packet.WindowSize = 65535;
                TcpPacketSend?.Invoke(this, new ProtocolIPArgs(packet, RemoteAddress, null));
                TcpState = ETcpState.SYN_SENTY;
                WaitHandle.Reset();
                WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                if (TcpState != ETcpState.ESTABLISHED)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
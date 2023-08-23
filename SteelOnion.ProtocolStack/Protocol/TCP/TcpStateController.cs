using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace SteelOnion.ProtocolStack.Protocol.TCP
{
    /// <summary>
    /// TCP状态机
    /// </summary>
    internal class TcpStateController
    {

        #region Private Fields

        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 数据缓冲区长度
        /// </summary>
        private static int BufferLength = 65535;

        private static Random Random = new Random();

        /// <summary>
        /// 数据缓冲区
        /// </summary>
        private byte[] _buffer = new byte[65535];

        /// <summary>
        /// 缓冲区队尾位置
        /// </summary>
        private int _bufferPosition;

        private EventHandler<int> DisposeEvent;

        #endregion Private Fields

        #region Public Constructors

        public TcpStateController(EventHandler<ProtocolIPArgs> send, EventHandler<int> dispose, ushort localPort, IPEndPoint remote)
        {
            _bufferPosition = 0;
            _buffer = new byte[BufferLength];
            Seq = (uint)Random.Next();
            LocalPort = localPort;
            RemotePort = (ushort)remote.Port;
            RemoteAddress = remote.Address;
            TcpPacketSend = send;
            DisposeEvent = dispose;
            //新连接总是在关闭状态
            TcpState = ETcpState.CLOSED;
            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            TcpClient = new SimulatedTcpClient(localPort, remote, ClientDisposed, ClientSend, ClientConnect, ClientDisconnect);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler<ProtocolIPArgs> TcpPacketSend;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// TCP连接的当前状态
        /// </summary>
        public ETcpState TcpState { get; set; }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// 确认号
        /// </summary>
        internal uint Ack { get; private set; }

        /// <summary>
        /// 本地端口
        /// </summary>
        internal ushort LocalPort { get; }

        /// <summary>
        /// 远程端口
        /// </summary>
        internal ushort RemotePort { get; }

        /// <summary>
        /// 序列号
        /// </summary>
        internal uint Seq { get; private set; }

        internal SimulatedTcpClient TcpClient { get; }

        internal EventWaitHandle WaitHandle { get; private set; }

        #endregion Internal Properties

        #region Private Properties

        private IPAddress RemoteAddress { get; }

        #endregion Private Properties

        #region Public Methods

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
                WaitHandle.WaitOne(ConnectTimeout);
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

        public void Disconnect()
        {
            if (TcpState == ETcpState.ESTABLISHED)
            {
                //开始四次挥手
                TcpPacket packet = new TcpPacket(LocalPort, RemotePort);
                packet.SequenceNumber = Seq;
                packet.Acknowledgment = true;
                packet.Finished = true;
                packet.AcknowledgmentNumber = Ack;
                packet.WindowSize = 65535;
                TcpPacketSend?.Invoke(this, new ProtocolIPArgs(packet, RemoteAddress, null));
                TcpState = ETcpState.FIN_WAIT_1;
                WaitHandle.Reset();
                WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            }
        }

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
                                TcpClient.EnqueuePacket(_buffer.Take(_bufferPosition).ToArray());
                                _bufferPosition = 0;
                            }
                        }
                    }
                    break;

                case ETcpState.FIN_WAIT_1://主动断开
                    {
                        if (packet.Acknowledgment)
                        {
                            //收到确认包
                            TcpState = ETcpState.FIN_WAIT_2;
                        }
                    }
                    break;

                case ETcpState.FIN_WAIT_2://确认到断开
                    {
                        if (packet.Acknowledgment && packet.Finished)
                        {
                            ReturnAcknowledgmentPacket(packet);
                            TcpState = ETcpState.CLOSED;
                        }
                    }
                    break;

                case ETcpState.CLOSE_WAIT://等待关闭
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

        #endregion Public Methods

        #region Private Methods

        private void ClientConnect(object? sender, EventArgs e)
        {
            TcpClient.IsConnected = Connect();
        }

        private void ClientDisconnect(object? sender, EventArgs e)
        {
            Disconnect();
        }
        private void ClientDisposed(object? sender, EventArgs e)
        {
            //断开连接
            Disconnect();
            DisposeEvent.Invoke(this, LocalPort);
        }

        private void ClientSend(object? sender, byte[] e)
        {
            TcpPacket packet = new TcpPacket(LocalPort, RemotePort);
            packet.SequenceNumber = Seq;
            Seq += (uint)e.Length;
            packet.Acknowledgment = true;
            packet.Push = true;
            packet.AcknowledgmentNumber = Ack;
            packet.WindowSize = 65535;
            packet.PayloadData = e;
            TcpPacketSend(this, new ProtocolIPArgs(packet, RemoteAddress, null));
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

        #endregion Private Methods

    }
}
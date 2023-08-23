using System;
using System.Collections.Concurrent;
using System.Net;

namespace SteelOnion.ProtocolStack
{
    /// <summary>
    /// 模拟TCP客户端
    /// </summary>
    public class SimulatedTcpClient
    {
        #region Internal Fields

        internal BlockingCollection<byte[]> _packets;

        /// <summary>
        /// 连接
        /// </summary>
        internal EventHandler ConnectEvent;

        /// <summary>
        /// 断开连接
        /// </summary>
        internal EventHandler DisconnectEvent;

        /// <summary>
        /// 释放
        /// </summary>
        internal EventHandler DisposeEvent;

        /// <summary>
        /// 发送数据
        /// </summary>
        internal EventHandler<byte[]> SendEvent;

        #endregion Internal Fields



        #region Public Constructors

        public SimulatedTcpClient(int port, IPEndPoint remote, EventHandler disposed, EventHandler<byte[]> send, EventHandler connect, EventHandler disconnect)
        {
            IsConnected = false;
            Port = port;
            Remote = remote;
            DisposeEvent = disposed;
            SendEvent = send;
            ConnectEvent = connect;
            DisconnectEvent = disconnect;
            _packets = new BlockingCollection<byte[]>();
        }

        #endregion Public Constructors



        #region Public Properties

        public bool IsConnected { get; internal set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 对端地址
        /// </summary>
        public IPEndPoint Remote { get; }

        #endregion Public Properties



        #region Public Methods

        public bool Connect()
        {
            ConnectEvent(this, EventArgs.Empty);
            return IsConnected;
        }

        public void Dispose()
        {
            _packets.CompleteAdding();
            DisconnectEvent.Invoke(this, EventArgs.Empty);
            DisposeEvent.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 读取数据流
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        public byte[] Read()
        {
            var bytes = _packets.Take();
            return bytes;
        }

        /// <summary>
        /// 发送数据流
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public bool Send(byte[] data)
        {
            if (IsConnected)
            {
                SendEvent(this, data);
                return true;
            }
            return false;
        }

        #endregion Public Methods

        #region Internal Methods

        internal void EnqueuePacket(byte[] data)
        {
            _packets.Add(data);
        }

        #endregion Internal Methods
    }
}
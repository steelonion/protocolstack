using PacketDotNet;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace SteelOnion.ProtocolStack
{
    /// <summary>
    /// 模拟TCP客户端
    /// </summary>
    public class SimulatedTcpClient
    {
        internal EventWaitHandle WaitHandle { get; }
        private static Random Random = new Random();

        /// <summary>
        /// 对端地址
        /// </summary>
        public IPEndPoint Remote { get; }

        /// <summary>
        /// 检查ACK是否正确
        /// </summary>
        /// <param name="ack"></param>
        /// <returns></returns>
        internal bool CheckAck(uint ack)
        {
            return ack == Seq + 1;
        }

        internal uint Seq { get; set; }

        internal bool Established { get; set; }

        /// <summary>
        /// 当前实例已经被释放
        /// </summary>
        internal Action<int> Disposed;

        internal Func<int, byte[], bool> SendFunc;

        internal Func<int, IPEndPoint, bool> ConnectFunc;

        internal BlockingCollection<TcpPacket> _packets;

        public SimulatedTcpClient(int port, IPEndPoint remote, Action<int> disposed, Func<int, byte[], bool> sendFunc, Func<int, IPEndPoint, bool> connectFunc)
        {
            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            Established = false;
            Port = port;
            Remote = remote;
            Disposed = disposed;
            SendFunc = sendFunc;
            ConnectFunc = connectFunc;
            _packets = new BlockingCollection<TcpPacket>();
            Seq = (uint)Random.Next();
        }

        public bool Connect()
        {
            return ConnectFunc(Port, Remote);
        }

        internal void EnqueuePacket(TcpPacket packet)
        {
            _packets.Add(packet);
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 发送数据流
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public bool Send(byte[] data)
        {
            if (!Established) return false;
            return SendFunc(Port, data);
        }

        /// <summary>
        /// 读取数据流
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        public byte[] Read()
        {
            var packet = _packets.Take();
            return packet.PayloadData;
        }

        public void Dispose()
        {
            _packets.CompleteAdding();
            Disposed?.Invoke(Port);
        }
    }
}
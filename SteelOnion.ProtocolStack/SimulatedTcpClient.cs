using PacketDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack
{
    internal class SimulatedTcpClient
    {
        static Random Random = new Random(DateTime.Now.Millisecond);

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
            return ack == _seq + 1;
        }

        private uint _seq;
        internal uint Seq => _seq++;

        internal bool Established { get; set; }

        /// <summary>
        /// 当前实例已经被释放
        /// </summary>
        internal Action<int> Disposed;

        internal Func<int, byte[], bool> SendFunc;

        internal BlockingCollection<TcpPacket> _packets;

        public SimulatedTcpClient(int port, Action<int> disposed, Func<int, byte[], bool> sendFunc)
        {
            Established = false;
            Port = port;
            Disposed = disposed;
            SendFunc = sendFunc;
            _packets = new BlockingCollection<TcpPacket>();
            _seq = (uint)Random.Next();
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

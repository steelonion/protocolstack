using PacketDotNet;
using SteelOnion.ProtocolStack.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack
{
    /// <summary>
    /// 模拟UDP客户端
    /// </summary>
    public class SimulatedUdpClient:IDisposable
    {
        /// <summary>
        /// 当前实例已经被释放
        /// </summary>
        internal Action<int> Disposed;

        internal Func<int,byte[], IPEndPoint, bool> SendFunc;

        internal BlockingCollection<UdpPacket> _packets;

        public SimulatedUdpClient(int port, Action<int> disposed, Func<int, byte[], IPEndPoint, bool> sendFunc)
        {
            Port = port;
            Disposed = disposed;
            SendFunc = sendFunc;
            _packets = new BlockingCollection<UdpPacket>();
        }

        internal void EnqueuePacket(UdpPacket packet)
        {
            _packets.Add(packet);
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 发送数据报
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public bool Send(byte[] data, IPEndPoint remote)
        {
            return SendFunc(Port, data, remote);
        }

        /// <summary>
        /// 读取数据报
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        public byte[] Read(out IPEndPoint? remote)
        {
            remote = null;
            var packet = _packets.Take();
            if(packet.ParentPacket is IPPacket ip)
            {
                remote = new IPEndPoint(ip.SourceAddress, packet.SourcePort);
                return packet.PayloadData;
            }
            return new byte[0];
        }

        public void Dispose()
        {
            _packets.CompleteAdding();
            Disposed?.Invoke(Port);
        }
    }
}

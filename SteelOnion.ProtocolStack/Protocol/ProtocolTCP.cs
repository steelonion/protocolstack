using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolTCP : ProtocolBase<TcpPacket, ProtocolIPArgs>
    {

        private Dictionary<int, SimulatedTcpClient> _ports;

        public ProtocolTCP(ProtocolStackConfig config) : base(config)
        {
            _ports = new Dictionary<int, SimulatedTcpClient>();
        }

        public override string ProtocolName => "TCP";

        /// <summary>
        /// 发起一个tcp连接请求
        /// </summary>
        /// <param name="port"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        private bool Connect(int port,IPEndPoint remote)
        {
            if (_ports.TryGetValue(port,out var client))
            {
                //开始三次握手
                TcpPacket packet = new TcpPacket((ushort)port, (ushort)remote.Port);
                packet.SequenceNumber = client.Seq;
                packet.Synchronize=true;
                SendPacket?.Invoke(this, new ProtocolIPArgs(packet, remote.Address, null));
                //block
                return true;
            }
            else
            {
                return false;
            }
        }

        internal SimulatedTcpClient CreateClient(int port)
        {
            if (port < 0) { throw new ArgumentOutOfRangeException("port"); }
            if (port > 65534) { throw new ArgumentOutOfRangeException("port"); }
            if (_ports.ContainsKey(port)) { throw new InvalidOperationException("port has used"); }
            return _ports[port] = new SimulatedTcpClient(port, RemoveClient, ClientSendPacket);
        }

        private bool ClientSendPacket(int arg1, byte[] arg2)
        {
            throw new NotImplementedException();
        }

        private void RemoveClient(int port)
        {
            //开始四次挥手
            _ports.Remove(port);
        }

        public override event EventHandler<ProtocolIPArgs>? SendPacket;

        public override void ReceivePacket(TcpPacket packet)
        {
            if (_ports.TryGetValue(packet.DestinationPort, out SimulatedTcpClient? client))
            {
                //服务器端确认报文
                if (packet.Synchronize && packet.Acknowledgment)
                {
                    //此处应当验证
                    //发送应答
                    var retPacket = new TcpPacket((ushort)client.Port, (ushort)client.Remote.Port);
                    retPacket.Acknowledgment = true;
                    retPacket.AcknowledgmentNumber = packet.SequenceNumber + 1;
                    SendPacket?.Invoke(this, new ProtocolIPArgs(packet, client.Remote.Address, null));
                    //客户端进入可通讯
                    client.Established = true;
                }
                else
                {
                    client.EnqueuePacket(packet);
                }
            }
        }
    }
}

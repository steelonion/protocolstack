using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            config.TcpModule = this;
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
                packet.WindowSize = 65535;
                SendPacket?.Invoke(this, new ProtocolIPArgs(packet, remote.Address, null));
                client.WaitHandle.Reset();
                client.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                return true;
            }
            else
            {
                return false;
            }
        }

        internal SimulatedTcpClient CreateClient(int port,IPEndPoint remote)
        {
            if (port < 0) { throw new ArgumentOutOfRangeException("port"); }
            if (port > 65534) { throw new ArgumentOutOfRangeException("port"); }
            if (_ports.ContainsKey(port)) { throw new InvalidOperationException("port has used"); }
            return _ports[port] = new SimulatedTcpClient(port, remote,RemoveClient, ClientSendPacket, Connect);
        }

        private bool ClientSendPacket(int port, byte[] data)
        {
            if (_ports.TryGetValue(port, out SimulatedTcpClient? client))
            {
                TcpPacket packet = new TcpPacket((ushort)port, (ushort)client.Remote.Port);
                packet.PayloadData = data;
                client.Seq += (uint)data.Length;
                packet.SequenceNumber = client.Seq;
                packet.Acknowledgment = true;
                packet.WindowSize = 65535;
                SendPacket?.Invoke(this, new ProtocolIPArgs(packet, client.Remote.Address, null));
                return true;
            }
            return false;
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
                    client.Seq += 1;
                    retPacket.SequenceNumber = client.Seq;
                    retPacket.WindowSize = 65535;
                    SendPacket?.Invoke(this, new ProtocolIPArgs(retPacket, client.Remote.Address, null));
                    //客户端进入可通讯
                    client.Established = true;
                    //停止阻塞
                    client.WaitHandle.Set();
                }
                if (packet.Finished)
                {
                    //开始挥手

                }
                else
                {
                    client.EnqueuePacket(packet);
                    var retPacket = new TcpPacket((ushort)client.Port, (ushort)client.Remote.Port);
                    retPacket.SequenceNumber = client.Seq;
                    retPacket.Acknowledgment = true;
                    retPacket.AcknowledgmentNumber = packet.SequenceNumber + (uint)packet.PayloadData.Length;
                    SendPacket?.Invoke(this, new ProtocolIPArgs(packet, client.Remote.Address, null));
                }
            }
        }
    }
}

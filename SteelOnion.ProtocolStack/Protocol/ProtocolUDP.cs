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
    internal class ProtocolUDP : ProtocolBase<UdpPacket, ProtocolIPArgs>
    {
        private Dictionary<int, SimulatedUdpClient> _ports;

        public ProtocolUDP(ProtocolStackConfig config) : base(config)
        {
            _ports=new Dictionary<int, SimulatedUdpClient>();
            config.UdpModule = this;
        }

        internal SimulatedUdpClient CreateClient(int port)
        {
            if (port < 0) { throw new ArgumentOutOfRangeException("port"); }
            if (port > 65534) { throw new ArgumentOutOfRangeException("port"); }
            if(_ports.ContainsKey(port)) { throw new InvalidOperationException("port has used"); }
            return _ports[port] = new SimulatedUdpClient(port, RemoveClient, ClientSendPacket);
        }

        private bool ClientSendPacket(int port, byte[] data, IPEndPoint point)
        {
            UdpPacket packet = new UdpPacket((ushort)port, (ushort)point.Port);
            packet.PayloadData = data;
            SendPacket?.Invoke(this, new ProtocolIPArgs(packet, point.Address, null));
            return true;
        }

        private void RemoveClient(int port)
        {
                _ports.Remove(port);
        }


        public override string ProtocolName => "UDP";

        public override event EventHandler<ProtocolIPArgs>? SendPacket;

        public override void ReceivePacket(UdpPacket packet)
        {
            if(_ports.TryGetValue(packet.DestinationPort, out SimulatedUdpClient? client))
            {
                client.EnqueuePacket(packet);
            }
        }
    }
}

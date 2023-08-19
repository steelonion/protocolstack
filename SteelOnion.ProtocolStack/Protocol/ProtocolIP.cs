using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Net;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolIP : ProtocolBase<IPPacket, ProtocolEthernetArgs>
    {
        private ProtocolICMP icmpModule;
        private ProtocolUDP udpModule;
        private ProtocolTCP tcpModule;

        public ProtocolIP(ProtocolStackConfig config) : base(config)
        {
            icmpModule = new ProtocolICMP(config);
            icmpModule.SendPacket += ProtocolSendProcess;
            udpModule = new ProtocolUDP(config);
            udpModule.SendPacket += ProtocolSendProcess;
            tcpModule = new ProtocolTCP(config);
            tcpModule.SendPacket += ProtocolSendProcess;
        }

        protected void ProtocolSendProcess(object? sender, ProtocolIPArgs args)
        {
            IPv4Packet packet = new IPv4Packet(Config.IPAddress, args.DstAddr);
            packet.PayloadPacket = args.Packet;
            packet.UpdateIPChecksum();
            if (args.Packet is TcpPacket tcp) { tcp.UpdateTcpChecksum(); }
            SendPacket?.Invoke(this, new ProtocolEthernetArgs(packet, null));
        }

        public override string ProtocolName => "IP";

        public override event EventHandler<ProtocolEthernetArgs>? SendPacket;

        public override void ReceivePacket(IPPacket packet)
        {
            //过滤包
            if (!packet.DestinationAddress.Equals(IPAddress.Broadcast) && !packet.DestinationAddress.Equals(Config.IPAddress)) return;
            switch (packet.Protocol)
            {
                case ProtocolType.Icmp:
                    {
                        if (packet.PayloadPacket is IcmpV4Packet icmp)
                        {
                            icmpModule.ReceivePacket(icmp);
                        }
                    }
                    break;

                case ProtocolType.Udp:
                    {
                        if (packet.PayloadPacket is UdpPacket udp) udpModule.ReceivePacket(udp);
                    }
                    break;

                case ProtocolType.Tcp:
                    {
                        if (packet.PayloadPacket is TcpPacket tcp) tcpModule.ReceivePacket(tcp);
                    }
                    break;
            }
        }
    }
}
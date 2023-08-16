using PacketDotNet;
using SteelOnion.ProtocolStack.Helper;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolIP : ProtocolBase<IPPacket,ProtocolEthernetArgs>
    {
        ProtocolICMP icmpModule;
        ProtocolUDP udpModule;
        public ProtocolIP(ProtocolStackConfig config) : base(config)
        {
            icmpModule = new ProtocolICMP(config);
            icmpModule.SendPacket += ProtocolSendProcess;
            udpModule = new ProtocolUDP(config);
            udpModule.SendPacket += ProtocolSendProcess;
        }

        protected void ProtocolSendProcess(object? sender, ProtocolIPArgs args)
        {
            IPv4Packet packet = new IPv4Packet(Config.IPAddress, args.DstAddr);
            packet.PayloadPacket = args.Packet;
            packet.UpdateIPChecksum();
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
                case PacketDotNet.ProtocolType.Icmp:
                    {
                        if (packet.PayloadPacket is IcmpV4Packet icmp)
                        {
                            icmpModule.ReceivePacket(icmp);
                        }
                    }
                    break;
                case PacketDotNet.ProtocolType.Udp:
                    {
                        if (packet.PayloadPacket is UdpPacket udp) udpModule.ReceivePacket(udp);
                    }
                    break;
            }
        }
    }
}

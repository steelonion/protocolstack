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
        RuntimeWatch _watch;
        ProtocolICMP icmpModule;
        public ProtocolIP(ProtocolStackConfig config) : base(config)
        {
            icmpModule = new ProtocolICMP(config);
            icmpModule.SendPacket += ProtocolSendProcess;
            _watch = new RuntimeWatch("IP");
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
            _watch.Start();
            //过滤包
            if (!packet.DestinationAddress.Equals(IPAddress.Broadcast) && !packet.DestinationAddress.Equals(Config.IPAddress)) return;
            _watch.Record();
            switch (packet.Protocol)
            {
                case PacketDotNet.ProtocolType.Icmp:
                    {
                        _watch.Record();
                        if (packet.PayloadPacket is IcmpV4Packet icmp)
                        {
                            icmpModule.ReceivePacket(icmp);
                        }
                        _watch.Record();
                    }
                    break;
            }
            _watch.Stop();
            Console.WriteLine(_watch);
        }
    }
}

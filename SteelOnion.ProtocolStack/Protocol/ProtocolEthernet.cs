using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolEthernet : ProtocolBase<EthernetPacket,PacketSendArgs>,IProtocolStackEntry
    {
        static PhysicalAddress Broadcast { get; } = PhysicalAddress.Parse("FF:FF:FF:FF:FF:FF");

        ProtocolArp arpModule;
        ProtocolIP ipModule;
        public ProtocolEthernet(ProtocolStackConfig config) : base(config)
        {
            arpModule=new ProtocolArp(config);
            arpModule.SendPacket += ProtocolSendProcess;
            ipModule = new ProtocolIP(config);
            ipModule.SendPacket += ProtocolSendProcess;
        }

        public override string ProtocolName => "Ethernet";

        public override event EventHandler<PacketSendArgs>? SendPacket;

        public override void ReceivePacket(EthernetPacket packet)
        {
            Stopwatch sw= Stopwatch.StartNew();
            if (!packet.DestinationHardwareAddress.Equals(Broadcast) && !packet.DestinationHardwareAddress.Equals(Config.MacAddress)) return;
            var e1 = sw.ElapsedTicks;
            long e2 = 0;
            long e3 = 0;
            switch (packet.Type)
            {
                case EthernetType.IPv4:
                    {
                        e2= sw.ElapsedTicks;
                        if (packet.PayloadPacket is IPPacket ipPacket)
                        {
                            ipModule.ReceivePacket(ipPacket);
                        }
                        e3 = sw.ElapsedTicks;
                    }
                    break;
                case EthernetType.Arp:
                    {
                        if (packet.PayloadPacket is ArpPacket arpPacket)
                        {
                            arpModule.ReceivePacket(arpPacket);
                        }
                    }
                    break;
            }
            sw.Stop();
            Console.WriteLine($"E1 {e1}");
            Console.WriteLine($"E2 {e2}");
            Console.WriteLine($"E3 {e3}");
            Console.WriteLine($"E4 {sw.ElapsedTicks}");
        }

        protected void ProtocolSendProcess(object? sender, ProtocolEthernetArgs args)
        {
            //默认广播包
            EthernetPacket packet = new EthernetPacket(Config.MacAddress, Broadcast, EthernetType.None);
            packet.PayloadPacket = args.Packet;
            if (packet.SourceHardwareAddress == PhysicalAddress.None)
            {
                packet.SourceHardwareAddress = Config.MacAddress;
            }
            //填充目标IP地址
            if (packet.PayloadPacket is IPPacket ip)
            {
                if (arpModule.ArpCache.TryGetValue(ip.DestinationAddress, out PhysicalAddress? dstMac))
                {
                    packet.DestinationHardwareAddress = dstMac;
                }
            }
            if (packet.PayloadPacket is ArpPacket arp)
            {
                if (arpModule.ArpCache.TryGetValue(arp.TargetProtocolAddress, out PhysicalAddress? dstMac))
                {
                    packet.DestinationHardwareAddress = dstMac;
                }
            }
            SendPacket?.Invoke(this, new PacketSendArgs(packet));
        }
    }
}

using PacketDotNet;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolEthernet : ProtocolBase<EthernetPacket,PacketSendArgs>,IProtocolStackEntry
    {
        internal static PhysicalAddress Broadcast { get; } = PhysicalAddress.Parse("FF:FF:FF:FF:FF:FF");
        internal static PhysicalAddress Zero { get; } = PhysicalAddress.Parse("00:00:00:00:00:00");

        Timer _resendTimer;
        TimeSpan timeoutSpan;

        ProtocolArp arpModule;
        ProtocolIP ipModule;
        public ProtocolEthernet(ProtocolStackConfig config) : base(config)
        {
            timeoutSpan=TimeSpan.FromSeconds(1);
            _waitQueue = new Queue<ProtocolEthernetArgs>();
            _resendTimer = new Timer(ResendQueue, null, 1000, 1000);
            arpModule=new ProtocolArp(config);
            arpModule.SendPacket += ProtocolSendProcess;
            ipModule = new ProtocolIP(config);
            ipModule.SendPacket += ProtocolSendProcess;
        }

        private void ResendQueue(object? state)
        {
            if (_waitQueue.Count == 0) return;
            while (_waitQueue.TryPeek(out var argsPeek))
            {
                if (argsPeek.WaitTime > timeoutSpan)
                {
                    var args = _waitQueue.Dequeue();
                    ProtocolSendProcess(this, args);
                    if (_waitQueue.Count == 0) break;
                }
            }
        }

        public override string ProtocolName => "Ethernet";

        public override event EventHandler<PacketSendArgs>? SendPacket;

        public override void ReceivePacket(EthernetPacket packet)
        {
            if (!packet.DestinationHardwareAddress.Equals(Broadcast) && !packet.DestinationHardwareAddress.Equals(Config.MacAddress)) return;
            switch (packet.Type)
            {
                case EthernetType.IPv4:
                    {
                        if (packet.PayloadPacket is IPPacket ipPacket)
                        {
                            ipModule.ReceivePacket(ipPacket);
                        }
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
        }

        Queue<ProtocolEthernetArgs> _waitQueue;

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
                else
                {
                    //开始探测mac
                    arpModule.Req(ip.DestinationAddress);
                    _waitQueue.Enqueue(args);
                    return;
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

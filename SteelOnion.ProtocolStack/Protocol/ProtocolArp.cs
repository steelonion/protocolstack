using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System.Net.Sockets;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolArp : ProtocolBase<ArpPacket,ProtocolEthernetArgs>
    {

        public Dictionary<IPAddress, PhysicalAddress> ArpCache { get; }
        public ProtocolArp(ProtocolStackConfig config) : base(config)
        {
            ArpCache = new Dictionary<IPAddress, PhysicalAddress>();
        }

        public override string ProtocolName => "ARP";

        public override event EventHandler<ProtocolEthernetArgs>? SendPacket;

        internal void Req(IPAddress address)
        {
            ArpPacket receivePacket = new ArpPacket(
            ArpOperation.Request,
            ProtocolEthernet.Zero, address,
                Config.MacAddress, Config.IPAddress);
            SendPacket?.Invoke(this, new ProtocolEthernetArgs(receivePacket, null));
        }

        public override void ReceivePacket(ArpPacket packet)
        {
            //ARP请求
            if(packet.Operation== ArpOperation.Request)
            {
                //是否请求自己的IP地址
                if (packet.TargetProtocolAddress.Equals(Config.IPAddress))
                {
                    //将地址信息添加到缓冲表里
                    ArpCache[packet.SenderProtocolAddress] = packet.SenderHardwareAddress;
                    //组包
                    ArpPacket receivePacket = new ArpPacket(
                        ArpOperation.Response,
                        packet.SenderHardwareAddress, packet.SenderProtocolAddress, 
                        Config.MacAddress, Config.IPAddress);
                    SendPacket?.Invoke(this, new ProtocolEthernetArgs(receivePacket, null));
                }
            }
            if(packet.Operation== ArpOperation.Response)
            {
                //是否请求自己的IP地址
                if (packet.TargetProtocolAddress.Equals(Config.IPAddress))
                {
                    //将地址信息添加到缓冲表里
                    ArpCache[packet.SenderProtocolAddress] = packet.SenderHardwareAddress;
                }
            }
        }
    }
}

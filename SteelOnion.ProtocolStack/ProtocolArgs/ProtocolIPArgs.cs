using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.ProtocolArgs
{
    /// <summary>
    /// 对IP层使用的参数
    /// </summary>
    internal class ProtocolIPArgs : ProtocolEthernetArgs
    {
        public ProtocolIPArgs(Packet packet, IPAddress dst, PhysicalAddress? dstMac) : base(packet, dstMac)
        {
            DstAddr = dst;
        }

        public IPAddress DstAddr { get; set; }
    }
}

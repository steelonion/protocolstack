using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.ProtocolArgs
{
    /// <summary>
    /// 以太网参数
    /// </summary>
    internal class ProtocolEthernetArgs : ProtocolArgsBase
    {
        public PhysicalAddress? DstMAC { get; set; }
        public ProtocolEthernetArgs(Packet packet,PhysicalAddress? physicalAddress) : base(packet)
        {
            DstMAC = physicalAddress;
        }
    }
}

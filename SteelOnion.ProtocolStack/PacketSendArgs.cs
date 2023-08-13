using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack
{
    /// <summary>
    /// 协议栈对外发送参数
    /// </summary>
    public class PacketSendArgs : EventArgs
    {
        public PacketSendArgs(EthernetPacket packet) { Packet = packet; }
        public EthernetPacket Packet { get; set; }
    }
}

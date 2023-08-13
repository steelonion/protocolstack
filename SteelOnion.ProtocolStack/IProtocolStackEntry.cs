using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack
{
    public interface IProtocolStackEntry:IProtocolStack<EthernetPacket, PacketSendArgs>
    {
    }
}

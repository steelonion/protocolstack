using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.ProtocolArgs
{
    internal abstract class ProtocolArgsBase:EventArgs
    {
        protected ProtocolArgsBase(Packet packet)
        {
            Packet = packet;
        }

        public Packet Packet { get; set; }
    }
}

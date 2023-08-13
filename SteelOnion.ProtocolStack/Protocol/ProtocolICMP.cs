using PacketDotNet;
using PacketDotNet.Utils;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolICMP : ProtocolBase<IcmpV4Packet,ProtocolIPArgs>
    {
        public ProtocolICMP(ProtocolStackConfig config) : base(config)
        {
        }

        public override string ProtocolName =>"ICMP";

        public override event EventHandler<ProtocolIPArgs>? SendPacket;

        public override void ReceivePacket(IcmpV4Packet packet)
        {
            if (packet.ParentPacket is IPPacket ipPacket)
            {
                switch (packet.TypeCode)
                {
                    //Ping Request
                    case IcmpV4TypeCode.EchoRequest:
                        {
                            var reply = new IcmpV4Packet(new ByteArraySegment(new byte[packet.TotalPacketLength]));
                            reply.Id = packet.Id;
                            reply.Sequence = packet.Sequence;
                            reply.Data = packet.Data;
                            reply.TypeCode = IcmpV4TypeCode.EchoReply;
                            reply.UpdateIcmpChecksum();
                            SendPacket?.Invoke(this, new ProtocolIPArgs(reply, ipPacket.SourceAddress, null));
                        }
                        break;
                }
            }
        }
    }
}

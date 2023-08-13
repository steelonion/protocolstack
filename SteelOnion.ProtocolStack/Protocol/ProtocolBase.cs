using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal abstract class ProtocolBase<T,Args> : IProtocolStack<T,Args> where T : Packet where Args : EventArgs
    {
        protected ProtocolStackConfig Config { get; }
        protected ProtocolBase(ProtocolStackConfig config)
        {
            Config = config;
        }


        public abstract string ProtocolName { get; }

        public abstract event EventHandler<Args>? SendPacket;

        public abstract void ReceivePacket(T packet);
    }
}

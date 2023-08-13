using PacketDotNet;
using SteelOnion.ProtocolStack.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack
{
    public class ProtocolStackFactory
    {
        public static ProtocolStackFactory Instance { get; } = new ProtocolStackFactory();
        private ProtocolStackFactory() { }
        public IProtocolStackEntry GetProtocolStack(ProtocolStackConfig config)
        {
            if(config == null)
            {
                throw new ArgumentNullException("config");
            }
            return new ProtocolEthernet(config);
        }
    }
}

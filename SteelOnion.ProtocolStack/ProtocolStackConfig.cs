using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack
{
    public class ProtocolStackConfig
    {
        public PhysicalAddress MacAddress { get; set; }
        public IPAddress IPAddress { get; set; }

        public ProtocolStackConfig()
        {
            MacAddress = PhysicalAddress.None;
            IPAddress = IPAddress.None;
        }
    }
}

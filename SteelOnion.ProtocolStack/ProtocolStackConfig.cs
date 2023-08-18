using SteelOnion.ProtocolStack.Protocol;
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

        internal ProtocolUDP? UdpModule { get; set; }
        internal ProtocolTCP? TcpModule { get; set; }

        public ProtocolStackConfig()
        {
            MacAddress = PhysicalAddress.None;
            IPAddress = IPAddress.None;
        }

        public SimulatedUdpClient CreateUdpClient(int port)
        {
            if (UdpModule == null)
            {
                throw new InvalidOperationException("UDP Protocol Stack Not Found");
            }
            return UdpModule.CreateClient(port);
        }

        public SimulatedTcpClient CreateTcpClient(int port,IPEndPoint remote)
        {
            if (TcpModule == null)
            {
                throw new InvalidOperationException("TCP Protocol Stack Not Found");
            }
            return TcpModule.CreateClient(port, remote);
        }
    }
}

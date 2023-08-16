using PacketDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.CLI
{
    internal class ProtocolStackHost
    {
        ConcurrentQueue<EthernetPacket> packets;
        internal event EventHandler<PacketSendArgs>? SendPacket;
        IProtocolStackEntry protocolStack;

        ProtocolStackConfig config;

        public ProtocolStackHost(PhysicalAddress mac,IPAddress ip)
        {
            packets = new ConcurrentQueue<EthernetPacket>();
            config = new ProtocolStackConfig();
            config.MacAddress = mac;
            config.IPAddress = ip;
            protocolStack = ProtocolStackFactory.Instance.GetProtocolStack(config);
            protocolStack.SendPacket += ProtocolStack_SendPacket;
        }

        public SimulatedUdpClient GetUdpClient(int port)
        {
            return config.CreateUdpClient(port);
        }

        public void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (packets.TryDequeue(out var packet))
                    {
                        protocolStack?.ReceivePacket(packet);
                    }
                }
            });
        }

        private void ProtocolStack_SendPacket(object? sender, PacketSendArgs e)
        {
            SendPacket?.Invoke(sender, e);
        }

        public bool Enqueue(EthernetPacket packet)
        {
            if (packets.Count < 1000)
            {
                packets.Enqueue(packet);
                return true;
            }
            return false;
        }
    }
}

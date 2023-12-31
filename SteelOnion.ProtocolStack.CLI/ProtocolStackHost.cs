﻿using PacketDotNet;
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
        BlockingCollection<EthernetPacket> packets;
        internal event EventHandler<PacketSendArgs>? SendPacket;
        IProtocolStackEntry protocolStack;

        ProtocolStackConfig config;

        public ProtocolStackHost(PhysicalAddress mac,IPAddress ip)
        {
            packets = new BlockingCollection<EthernetPacket>();
            config = new ProtocolStackConfig();
            config.MacAddress = mac;
            config.IPAddress = ip;
            protocolStack = ProtocolStackFactory.Instance.GetProtocolStack(config);
            protocolStack.SendPacket += ProtocolStack_SendPacket;
        }

        public SimulatedTcpClient GetTcpClient(int port,IPEndPoint remote)
        {
            return config.CreateTcpClient(port, remote);
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
                    var packet = packets.Take();
                    protocolStack?.ReceivePacket(packet);
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
                packets.Add(packet);
                return true;
            }
            return false;
        }
    }
}

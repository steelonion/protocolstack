using Microsoft.VisualStudio.TestTools.UnitTesting;
using PacketDotNet;
using SharpPcap;
using SteelOnion.ProtocolStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Tests
{
    [TestClass()]
    public class ProtocolStackFactoryTests
    {
        class NetDeviceHost : IDisposable
        {
            internal PhysicalAddress DevMac => dev.MacAddress;
            internal event Action<EthernetPacket>? PacketReceived;
            ILiveDevice dev;
            string DevKeyWord = "VirtualBox";
            public NetDeviceHost()
            {
                var devices = CaptureDeviceList.Instance.ToList();
                dev = devices.First(x => x.ToString().Contains(DevKeyWord));
                dev.OnPacketArrival += Dev_OnPacketArrival;
                dev.Open(DeviceModes.Promiscuous);
                dev.StartCapture();
            }

            public void Dispose()
            {
                PacketReceived = null;
                dev.Dispose();

            }

            private void Dev_OnPacketArrival(object sender, PacketCapture e)
            {
                if (e.GetPacket().GetPacket() is EthernetPacket packet)
                {
                    PacketReceived?.Invoke(packet);
                }
            }

            internal void ProtocolStack_SendPacket(object? sender, PacketSendArgs e)
            {
                dev.SendPacket(e.Packet);
            }

            ~NetDeviceHost()
            {
                Dispose();
            }

        }

        [TestMethod()]
        public void TcpSendTest()
        {
            var dev = new NetDeviceHost();
            var config = new ProtocolStackConfig();
            config.MacAddress = dev.DevMac;
            config.IPAddress = IPAddress.Parse("192.168.56.3");
            var protocolStack = ProtocolStackFactory.Instance.GetProtocolStack(config);
            protocolStack.SendPacket += dev.ProtocolStack_SendPacket;
            dev.PacketReceived += protocolStack.ReceivePacket;
            var client = config.CreateTcpClient(7086, IPEndPoint.Parse("192.168.56.101:7086"));
            client.Connect();
            client.Send(Encoding.ASCII.GetBytes("Hello"));
            Thread.Sleep(1000);
            client.Send(Encoding.ASCII.GetBytes("I'm a simple protocol stack"));
            Thread.Sleep(1000);
            client.Send(Encoding.ASCII.GetBytes("Byebye!"));
            client.Dispose();
        }
    }
}
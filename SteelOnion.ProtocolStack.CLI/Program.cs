using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace SteelOnion.ProtocolStack.CLI
{
    internal class Program
    {
        static ILiveDevice? capDevice;
        static ProtocolStackHost? host;
        static void Main(string[] args)
        {
            var devices = CaptureDeviceList.Instance.ToList();
            for (int i = 0; i < devices.Count; i++)
            {
                ILiveDevice device = devices[i];
                Console.WriteLine($"{i}:");
                Console.WriteLine($"{device}");
            }
            Console.WriteLine("Pls input device number");
            var num= Console.ReadLine();
            if(int.TryParse(num,out int devNum))
            {
                capDevice = devices[devNum];
                host = new ProtocolStackHost(capDevice.MacAddress, IPAddress.Parse("192.168.3.24"));
                host.SendPacket += ProtocolStack_SendPacket;
                capDevice.OnPacketArrival += Device_OnPacketArrival;
                capDevice.Open(mode: DeviceModes.Promiscuous);
                capDevice.StartCapture();
                host.Start();
                var udp= host.GetUdpClient(8888);
                Task.Factory.StartNew(() => ReadUdp(udp));
                while (true)
                {
                    var cmd = Console.ReadLine();
                    if (cmd == "exit") break;
                    udp.Send(new byte[] { 30, 31, 32, 33, 34, 35 }, IPEndPoint.Parse("192.168.3.4:8888"));
                }
                udp.Dispose();
                capDevice.StopCapture();
                capDevice.Close();
            }
        }

        public static void ReadUdp(SimulatedUdpClient udp)
        {
            while (true)
            {
                var data= udp.Read(out var remote);
                Console.WriteLine($"{remote}>>{BitConverter.ToString(data)}");
            }
        }

        private static void ProtocolStack_SendPacket(object? sender, PacketSendArgs e)
        {
            capDevice?.SendPacket(e.Packet);
        }

        private static void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var raw = e.GetPacket();
            if (raw.GetPacket() is EthernetPacket packet)
            {
                host?.Enqueue(packet);
            }
        }
    }
}
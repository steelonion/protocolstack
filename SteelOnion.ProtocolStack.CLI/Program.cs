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
                host = new ProtocolStackHost(capDevice.MacAddress, IPAddress.Parse("192.168.56.1"));
                host.SendPacket += ProtocolStack_SendPacket;
                capDevice.OnPacketArrival += Device_OnPacketArrival;
                capDevice.Open(mode: DeviceModes.Promiscuous);
                capDevice.StartCapture();
                host.Start();
                var tcp= host.GetTcpClient(8888,IPEndPoint.Parse("192.168.56.101:8888"));
                tcp.Connect();
                Task.Factory.StartNew(() => ReadTcp(tcp));
                while (true)
                {
                    var cmd = Console.ReadLine();
                    if (cmd == "exit") break;
                    tcp.Send(new byte[] { 30, 31, 32, 33, 34, 35 });
                }
                tcp.Dispose();
                capDevice.StopCapture();
                capDevice.Close();
            }
        }

        public static void ReadTcp(SimulatedTcpClient tcp)
        {
            while (true)
            {
                var data= tcp.Read();
                Console.WriteLine($"{BitConverter.ToString(data)}");
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
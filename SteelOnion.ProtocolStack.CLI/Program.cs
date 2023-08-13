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
                Console.WriteLine("Press any key to end");
                Console.ReadKey();
                capDevice.StopCapture();
                capDevice.Close();
            }
        }

        private static void ProtocolStack_SendPacket(object? sender, PacketSendArgs e)
        {
            capDevice?.SendPacket(e.Packet);
        }

        private static void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var raw = e.GetPacket();
            var tp1 = sw.ElapsedTicks;
            long tp2 = 0;
            long tp3 = 0;
            if (raw.GetPacket() is EthernetPacket packet)
            {
                tp2 = sw.ElapsedTicks;
                host?.Enqueue(packet);
                tp3 = sw.ElapsedTicks;
            }
            sw.Stop();
            Console.WriteLine($"T1 {tp1}");
            Console.WriteLine($"T2 {tp2}");
            Console.WriteLine($"T3 {tp3}");
            Console.WriteLine($"T4 {sw.ElapsedTicks}");
        }
    }
}
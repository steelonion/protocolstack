using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.ProtocolArgs
{
    /// <summary>
    /// 以太网参数
    /// </summary>
    internal class ProtocolEthernetArgs : ProtocolArgsBase
    {
        /// <summary>
        /// 当前包已经等待的时间
        /// </summary>
        public TimeSpan WaitTime => DateTimeOffset.Now - Timestamp;
        /// <summary>
        /// 包发送时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// 当前包是否是重发包
        /// </summary>
        public bool HasResend { get; set; }

        public PhysicalAddress? DstMAC { get; set; }
        public ProtocolEthernetArgs(Packet packet,PhysicalAddress? physicalAddress) : base(packet)
        {
            DstMAC = physicalAddress;
            Timestamp = DateTimeOffset.Now;
        }
    }
}

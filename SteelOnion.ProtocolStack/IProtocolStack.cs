using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacketDotNet;

namespace SteelOnion.ProtocolStack
{
    /// <summary>
    /// 协议栈处理接口
    /// </summary>
    public interface IProtocolStack<T,Args> where T: Packet where Args : EventArgs
    {
        /// <summary>
        /// ProtocolName
        /// </summary>
        string ProtocolName { get; }

        /// <summary>
        /// 从下层协议栈接收到数据包
        /// </summary>
        void ReceivePacket(T packet);

        /// <summary>
        /// 向下层协议栈发送数据包
        /// </summary>
        event EventHandler<Args>? SendPacket;
    }
}

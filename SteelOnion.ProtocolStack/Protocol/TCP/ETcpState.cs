using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Protocol.TCP
{
    internal enum ETcpState
    {
        //状态时初始状态
        CLOSED,
        //服务器端的状态监听
        LISTEN,
        //服务器端收到 SYN 后，状态为 SYN；发送 SYN ACK
        SYNRECVD,
        //应用程序发送 SYN 后，状态为 SYN_SENT
        SYN_SENTY,
        //SYNRECVD 收到 ACK 后，状态为 ESTABLISHED； SYN_SENT 在收到 SYN ACK，发送 ACK，状态为 ESTABLISHED；
        ESTABLISHED,
        //服务器端在收到 FIN 后，发送 ACK，状态为 CLOSE_WAIT；如果此时服务器端还有数据需要发送，那么就发送，直到数据发送完毕；此时，服务器端发送FIN，状态变为 LAST_ACK;
        CLOSE_WAIT,
        // FIN，准备断开 TCP 连接；状态从 ESTABLISHED -> FIN_WAIT_1；
        FIN_WAIT_1,
        //应用程序端只收到服务器端得 ACK 信号，并没有收到FIN信号；说明服务器端还有数据传输，那么此时为半连接；
        FIN_WAIT_2,

        TIME_WAIT
    }
}

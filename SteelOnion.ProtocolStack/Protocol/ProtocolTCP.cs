using PacketDotNet;
using SteelOnion.ProtocolStack.Protocol.TCP;
using SteelOnion.ProtocolStack.ProtocolArgs;
using System;
using System.Collections.Generic;
using System.Net;

namespace SteelOnion.ProtocolStack.Protocol
{
    internal class ProtocolTCP : ProtocolBase<TcpPacket, ProtocolIPArgs>
    {
        #region Private Fields

        private Dictionary<int, TcpStateController> _tcpStateControllers;

        #endregion Private Fields

        #region Public Constructors

        public ProtocolTCP(ProtocolStackConfig config) : base(config)
        {
            _tcpStateControllers = new Dictionary<int, TcpStateController>();
            config.TcpModule = this;
        }

        #endregion Public Constructors

        #region Public Events

        public override event EventHandler<ProtocolIPArgs>? SendPacket;

        #endregion Public Events

        #region Public Properties

        public override string ProtocolName => "TCP";

        #endregion Public Properties

        #region Public Methods

        public override void ReceivePacket(TcpPacket packet)
        {
            if (_tcpStateControllers.TryGetValue(packet.DestinationPort, out TcpStateController? client))
            {
                client.ReceiveTcpPacket(packet);
            }
        }

        #endregion Public Methods

        #region Internal Methods

        internal SimulatedTcpClient CreateClient(int port, IPEndPoint remote)
        {
            if (port < 0) { throw new ArgumentOutOfRangeException("port"); }
            if (port > 65534) { throw new ArgumentOutOfRangeException("port"); }
            if (_tcpStateControllers.ContainsKey(port)) { throw new InvalidOperationException("port has used"); }
            var tcpStateController = new TcpStateController(ClientSendPacket, RemoveClient, (ushort)port, remote);
            _tcpStateControllers[port] = tcpStateController;
            return tcpStateController.TcpClient;
        }

        #endregion Internal Methods

        #region Private Methods

        private void ClientSendPacket(object? sender, ProtocolIPArgs e)
        {
            SendPacket?.Invoke(this, e);
        }

        private void RemoveClient(object? sender, int e)
        {
            _tcpStateControllers.Remove(e);
        }

        #endregion Private Methods
    }
}
namespace SecondMonitor.Hub.Core.Demo
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using LiteNetLib;
    using LiteNetLib.Utils;
    using ProtoBuf;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Remote;
    using SecondMonitor.Remote.Models;

    /// <summary>
    /// Reproduction fidèle du serveur de diffusion de SecondMonitor (BroadCastServerController, côté Windows),
    /// pour tester le hub de bout en bout avec le vrai protocole réseau.
    /// </summary>
    public sealed class DemoBroadcastServer : IDisposable
    {
        private readonly EventBasedNetListener _listener = new();
        private readonly NetManager _server;
        private readonly object _lock = new();
        private bool _sessionStartSent;

        public DemoBroadcastServer()
        {
            _listener.ConnectionRequestEvent += r => r.AcceptIfKey(DatagramPayload.Version4);
            _listener.NetworkReceiveUnconnectedEvent += OnUnconnected;
            _listener.PeerConnectedEvent += p => Console.WriteLine($"[demo] client connecté {p.Address}:{p.Port}");
            _server = new NetManager(_listener, new PacketCompressionLayer()) { BroadcastReceiveEnabled = true, IPv6Enabled = false };
        }

        public int ConnectedClients
        {
            get { lock (_lock) { return _server.ConnectedPeersCount; } }
        }

        public bool Start(int port)
        {
            lock (_lock)
            {
                return _server.Start(port);
            }
        }

        public void Poll()
        {
            lock (_lock)
            {
                _server.PollEvents();
            }
        }

        public void Send(SimulatorDataSet data)
        {
            var payload = new DatagramPayload
            {
                ContainsOtherDriversTiming = true,
                ContainsPlayersTiming = true,
                ContainsSimulatorSourceInfo = true,
                DriversInfo = data.DriversInfo,
                InputInfo = data.InputInfo,
                LeaderInfo = data.LeaderInfo,
                PayloadKind = _sessionStartSent ? DatagramPayloadKind.Normal : DatagramPayloadKind.SessionStart,
                PlayerInfo = data.PlayerInfo,
                SessionInfo = data.SessionInfo,
                Source = data.Source,
                SimulatorSourceInfo = data.SimulatorSourceInfo,
            };

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, payload);
            var writer = new NetDataWriter();
            writer.Put(stream.ToArray());
            lock (_lock)
            {
                if (_server.ConnectedPeersCount == 0)
                {
                    return;
                }

                _server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
                _sessionStartSent = true;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _server.Stop();
            }
        }

        private void OnUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType != UnconnectedMessageType.Broadcast)
            {
                return;
            }

            if (reader.GetString() != DatagramPayload.Version4)
            {
                return;
            }

            var response = new NetDataWriter();
            response.Put(DatagramPayload.Version4);
            _server.SendBroadcast(response, remoteEndPoint.Port);
        }
    }
}

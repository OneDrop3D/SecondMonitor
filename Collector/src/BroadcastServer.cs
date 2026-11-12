namespace SecondMonitor.Collector
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using LiteNetLib;
    using LiteNetLib.Utils;
    using ProtoBuf;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.PluginsConfiguration.Common.Controller;
    using SecondMonitor.Remote;
    using SecondMonitor.Remote.Adapter;
    using SecondMonitor.Remote.Comparators;
    using SecondMonitor.Remote.Models;

    /// <summary>
    /// Serveur de diffusion, identique au plugin « Data Broadcast Server » de SecondMonitor (LiteNetLib + LZ4 + protobuf,
    /// clé de version 4, découverte automatique sur le réseau) : le hub ne voit aucune différence.
    /// Sans interface WPF et avec une boucle réseau peu gourmande (toutes les 15 ms au lieu de 5 ms).
    /// </summary>
    internal sealed class BroadcastServer : IDisposable
    {
        private readonly NetManager _server;
        private readonly EventBasedNetListener _listener = new();
        private readonly DatagramPayloadPacker _packer;
        private readonly ConcurrentQueue<NetPeer> _newPeers = new();
        private readonly Stopwatch _sinceLastPackage = Stopwatch.StartNew();
        private readonly object _sendLock = new();
        private Thread _loop;
        private volatile bool _running;
        private long _packages;
        private long _bytes;

        public BroadcastServer(IPluginSettingsProvider settingsProvider)
        {
            _packer = new DatagramPayloadPacker(settingsProvider, new SimulatorSourceInfoComparator());
            _server = new NetManager(_listener, new PacketCompressionLayer())
            {
                BroadcastReceiveEnabled = true,
                IPv6Enabled = false,
                UnconnectedMessagesEnabled = true,
                DisconnectTimeout = 10000,
            };
            _listener.ConnectionRequestEvent += request => request.AcceptIfKey(DatagramPayload.Version4);
            _listener.PeerConnectedEvent += peer =>
            {
                _newPeers.Enqueue(peer);
                PeerConnected?.Invoke(peer.Address.ToString());
            };
            _listener.PeerDisconnectedEvent += (peer, info) => PeerDisconnected?.Invoke(peer.Address.ToString(), info.Reason.ToString());
            _listener.NetworkReceiveUnconnectedEvent += OnDiscovery;
        }

        public event Action<string> PeerConnected;

        public event Action<string, string> PeerDisconnected;

        public int PeerCount => _server.ConnectedPeersCount;

        public string[] Peers => _server.ConnectedPeerList.Select(p => p.Address.ToString()).ToArray();

        public long PackagesSent => Interlocked.Read(ref _packages);

        public long BytesSent => Interlocked.Read(ref _bytes);

        public bool Start(int port)
        {
            if (!_server.Start(IPAddress.Any, IPAddress.IPv6Any, port))
            {
                return false;
            }

            _running = true;
            _loop = new Thread(Loop) { IsBackground = true, Name = "Réseau", Priority = ThreadPriority.BelowNormal };
            _loop.Start();
            return true;
        }

        public void SendSessionStarted(SimulatorDataSet data)
        {
            if (PeerCount == 0)
            {
                return;
            }

            Send(_packer.CreateSessionStartedPayload(data), null);
        }

        public void SendData(SimulatorDataSet data)
        {
            if (PeerCount == 0 || !_packer.IsMinimalPackageDelayPassed())
            {
                return;
            }

            while (_newPeers.TryDequeue(out NetPeer peer))
            {
                Send(_packer.CreateHandshakeDatagramPayload(), peer);
            }

            Send(_packer.CreateRegularDatagramPayload(data), null);
        }

        public void Dispose()
        {
            _running = false;
            _loop?.Join(500);
            _server.Stop();
        }

        private void Loop()
        {
            while (_running)
            {
                _server.PollEvents();
                if (PeerCount > 0 && _sinceLastPackage.ElapsedMilliseconds > 1000)
                {
                    // Signe de vie quand le jeu n'envoie rien (menus, chargement).
                    Send(_packer.CreateHearthBeatDatagramPayload(), null);
                }

                Thread.Sleep(15);
            }
        }

        private void OnDiscovery(IPEndPoint remote, NetPacketReader reader, UnconnectedMessageType type)
        {
            if (type != UnconnectedMessageType.Broadcast)
            {
                return;
            }

            try
            {
                if (reader.GetString() != DatagramPayload.Version4)
                {
                    return;
                }

                var response = new NetDataWriter();
                response.Put(DatagramPayload.Version4);
                _server.SendBroadcast(response, remote.Port);
                _server.SendUnconnectedMessage(response, remote);
            }
            catch (Exception)
            {
                // Paquet de découverte malformé : ignoré.
            }
            finally
            {
                reader.Recycle();
            }
        }

        private void Send(DatagramPayload payload, NetPeer peer)
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, payload);
                bytes = ms.ToArray();
            }

            var writer = new NetDataWriter(false, bytes.Length + 8);
            writer.Put(bytes);
            lock (_sendLock)
            {
                if (peer != null)
                {
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    _server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
                }
            }

            _sinceLastPackage.Restart();
            Interlocked.Increment(ref _packages);
            Interlocked.Add(ref _bytes, bytes.Length);
        }
    }
}

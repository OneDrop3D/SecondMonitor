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
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.PluginsConfiguration.Common.Controller;
    using SecondMonitor.Remote;
    using SecondMonitor.Remote.Adapter;
    using SecondMonitor.Remote.Comparators;
    using SecondMonitor.Remote.Models;

    /// <summary>
    /// Serveur de diffusion, identique au plugin « Data Broadcast Server » de SecondMonitor (LiteNetLib + LZ4 + protobuf,
    /// clé de version 4, découverte automatique sur le réseau) : le hub ne voit aucune différence.
    /// Sans interface WPF et avec une boucle réseau peu gourmande (toutes les 50 ms au lieu de 5 ms).
    /// Économies (format inchangé) : les signes de vie ne renvoient plus tout le plateau identique chaque seconde,
    /// et la sérialisation réutilise un seul tampon au lieu de trois copies par envoi.
    /// </summary>
    internal sealed class BroadcastServer : IDisposable
    {
        private readonly NetManager _server;
        private readonly EventBasedNetListener _listener = new();
        private readonly DatagramPayloadPacker _packer;
        private readonly ConcurrentQueue<NetPeer> _newPeers = new();
        private readonly Stopwatch _sinceLastPackage = Stopwatch.StartNew();
        private readonly object _sendLock = new();
        private readonly MemoryStream _buffer = new(64 * 1024); // protégé par _sendLock
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
                EnableStatistics = true, // octets réellement envoyés (après compression), pour l'affichage du débit
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

        /// <summary>Octets réellement partis sur le réseau (compressés, en-têtes compris).</summary>
        public long WireBytesSent => _server.Statistics.BytesSent;

        /// <summary>Paquets UDP réellement envoyés (fragments, accusés de réception et signes de vie compris).</summary>
        public long DatagramsSent => _server.Statistics.PacketsSent;

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

            lock (_sendLock)
            {
                Send(_packer.CreateSessionStartedPayload(data), null);
            }
        }

        public void SendData(SimulatorDataSet data)
        {
            if (PeerCount == 0 || !_packer.IsMinimalPackageDelayPassed())
            {
                return;
            }

            lock (_sendLock)
            {
                SendHandshakes();
                Send(_packer.CreateRegularDatagramPayload(data), null);
            }
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
                if (PeerCount > 0)
                {
                    lock (_sendLock)
                    {
                        // Hub arrivé pendant que le jeu se tait : il reçoit tout de suite le dernier état complet.
                        SendHandshakes();
                        if (_sinceLastPackage.ElapsedMilliseconds > 1000)
                        {
                            // Signe de vie quand le jeu n'envoie rien (menus, chargement, pause).
                            Send(LightHeartbeat(), null);
                        }
                    }
                }

                // Rien d'urgent ici (découverte, connexions, signe de vie à la seconde) : 20 réveils par seconde suffisent.
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Signe de vie allégé. Le hub, comme SecondMonitor, garde pour chaque collecteur le dernier plateau, le dernier joueur et
        /// la dernière description du jeu reçus (« Contains… » à faux = « inchangé ») : les renvoyer identiques chaque seconde,
        /// comme le faisait le plugin d'origine, coûtait jusqu'à 50 Ko par seconde de menus ou de pause pour rien.
        /// Les nouveaux hubs reçoivent d'abord l'état complet (SendHandshakes).
        /// </summary>
        private DatagramPayload LightHeartbeat()
        {
            DatagramPayload payload = _packer.CreateHearthBeatDatagramPayload();
            payload.ContainsOtherDriversTiming = false;
            payload.DriversInfo = Array.Empty<DriverInfo>();
            payload.LeaderInfo = null;
            payload.ContainsPlayersTiming = false;
            payload.PlayerInfo = null;
            payload.ContainsSimulatorSourceInfo = false;
            payload.SimulatorSourceInfo = null;
            return payload;
        }

        /// <summary>État complet (dernier jeu de données) aux hubs qui viennent de se connecter. Appelé sous _sendLock.</summary>
        private void SendHandshakes()
        {
            while (_newPeers.TryDequeue(out NetPeer peer))
            {
                Send(_packer.CreateHandshakeDatagramPayload(), peer);
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

        /// <summary>
        /// Envoie un paquet (protobuf brut, comme le plugin d'origine). Appelé sous _sendLock : le tampon est réutilisé,
        /// LiteNetLib le recopie dans ses propres paquets avant de rendre la main.
        /// </summary>
        private void Send(DatagramPayload payload, NetPeer peer)
        {
            _buffer.SetLength(0);
            Serializer.Serialize(_buffer, payload);
            byte[] bytes = _buffer.GetBuffer();
            int length = (int)_buffer.Length;
            if (peer != null)
            {
                peer.Send(bytes, 0, length, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                _server.SendToAll(bytes, 0, length, DeliveryMethod.ReliableOrdered);
            }

            if (_buffer.Capacity > 1024 * 1024)
            {
                _buffer.SetLength(0);
                _buffer.Capacity = 64 * 1024; // paquet exceptionnel : on ne garde pas un très gros tampon
            }

            _sinceLastPackage.Restart();
            Interlocked.Increment(ref _packages);
            Interlocked.Add(ref _bytes, length);
        }
    }
}

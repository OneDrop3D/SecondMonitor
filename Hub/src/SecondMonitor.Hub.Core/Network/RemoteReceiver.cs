namespace SecondMonitor.Hub.Core.Network
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteNetLib;
    using LiteNetLib.Utils;
    using ProtoBuf;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Remote;
    using SecondMonitor.Remote.Adapter;
    using SecondMonitor.Remote.Models;

    /// <summary>
    /// Client réseau compatible avec le plugin « Remote / Broadcast server » de SecondMonitor (Windows).
    /// Le PC de jeu diffuse ; le Mac reçoit. Découverte automatique sur le réseau local + adresses manuelles
    /// (utile quand le PC et le Mac ne sont pas sur le même sous-réseau, ou via Tailscale).
    /// </summary>
    public sealed class RemoteReceiver : IDisposable
    {
        public const int DefaultPort = 52642;

        private readonly object _lock = new();
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly int _port;
        private readonly Dictionary<IPEndPoint, IDatagramPayloadUnPacker> _unpackers = new();
        private readonly List<string> _manualHosts;
        private CancellationTokenSource _cts;
        private Task _loop;
        private DateTime _lastPacketUtc = DateTime.MinValue;
        private DateTime _lastDataUtc = DateTime.MinValue;
        private string _lastGame;
        private long _bytes;
        private long _bytesAtStatus;
        private DateTime _statusUtc = DateTime.UtcNow;
        private double _kbPerSecond;

        public RemoteReceiver(int port = DefaultPort, IEnumerable<string> manualHosts = null)
        {
            _port = port;
            _manualHosts = manualHosts?.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).ToList() ?? new List<string>();
            _listener = new EventBasedNetListener();
            _listener.ConnectionRequestEvent += r => r.AcceptIfKey(DatagramPayload.Version4);
            _listener.NetworkReceiveEvent += OnNetworkReceive;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
            _listener.PeerConnectedEvent += p =>
            {
                Log($"Connecté au PC de jeu {p.Address}:{p.Port}");
                Capture?.Split();
            };
            _listener.PeerDisconnectedEvent += (p, info) => Log($"Déconnecté de {p.Address}:{p.Port} ({info.Reason})");
            _client = new NetManager(_listener, new PacketCompressionLayer())
            {
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = false,
                DisconnectTimeout = 10000,
            };
        }

        /// <summary>Données reçues (déjà « dépaquetées » comme le fait SecondMonitor).</summary>
        public event Action<SimulatorDataSet, DatagramPayloadKind> DataReceived;

        public event Action<string> LogMessage;

        /// <summary>Enregistrement brut des paquets reçus (diagnostic, rejouable).</summary>
        public CaptureWriter Capture { get; set; }


        public IReadOnlyList<string> ManualHosts
        {
            get { lock (_lock) { return _manualHosts.ToList(); } }
        }

        public ReceiverStatus GetStatus()
        {
            lock (_lock)
            {
                var connected = _client.ConnectedPeerList.Where(p => p.ConnectionState == ConnectionState.Connected).ToList();
                return new ReceiverStatus
                {
                    Port = _port,
                    IsConnected = connected.Count > 0,
                    Peers = connected.Select(p => $"{p.Address}:{p.Port}").ToList(),
                    ManualHosts = _manualHosts.ToList(),
                    SecondsSinceLastPacket = _lastPacketUtc == DateTime.MinValue ? null : (DateTime.UtcNow - _lastPacketUtc).TotalSeconds,
                    SecondsSinceLastData = _lastDataUtc == DateTime.MinValue ? null : (DateTime.UtcNow - _lastDataUtc).TotalSeconds,
                    Game = _lastGame,
                    KbPerSecond = Math.Round(KbRate(), 1),
                    LocalAddresses = GetLocalAddresses(),
                };
            }
        }

        private double KbRate()
        {
            DateTime now = DateTime.UtcNow;
            double dt = (now - _statusUtc).TotalSeconds;
            if (dt >= 1)
            {
                long b = Interlocked.Read(ref _bytes);
                _kbPerSecond = (b - _bytesAtStatus) / dt / 1024;
                _bytesAtStatus = b;
                _statusUtc = now;
            }

            return _lastPacketUtc != DateTime.MinValue && (now - _lastPacketUtc).TotalSeconds > 3 ? 0 : _kbPerSecond;
        }

        public void SetManualHosts(IEnumerable<string> hosts)
        {
            lock (_lock)
            {
                _manualHosts.Clear();
                _manualHosts.AddRange(hosts.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).Distinct());
            }

            TryConnect();
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_cts != null)
                {
                    return;
                }

                _client.Start();
                _cts = new CancellationTokenSource();
                _loop = Task.Run(() => LoopAsync(_cts.Token));
            }

            Log($"Récepteur démarré (port local {_client.LocalPort}, serveur attendu sur le port {_port})");
        }

        public void Dispose()
        {
            _cts?.Cancel();
            try
            {
                _loop?.Wait(1000);
            }
            catch (AggregateException)
            {
            }

            lock (_lock)
            {
                _client.Stop();
            }
        }

        private async Task LoopAsync(CancellationToken token)
        {
            var lastAttempt = DateTime.MinValue;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    lock (_lock)
                    {
                        _client.PollEvents();
                    }

                    if (DateTime.UtcNow - lastAttempt > TimeSpan.FromSeconds(3))
                    {
                        lastAttempt = DateTime.UtcNow;
                        bool connected;
                        lock (_lock)
                        {
                            connected = _client.ConnectedPeerList.Any(p => p.ConnectionState == ConnectionState.Connected);
                        }

                        if (!connected)
                        {
                            TryConnect();
                        }
                    }

                    await Task.Delay(5, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log("Erreur réseau : " + ex.Message);
                    await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private void TryConnect()
        {
            lock (_lock)
            {
                // 1) Découverte automatique (broadcast) sur le réseau local.
                var writer = new NetDataWriter();
                writer.Put(DatagramPayload.Version4);
                _client.SendBroadcast(writer, _port);

                // 2) Adresses saisies à la main (autre sous-réseau, Tailscale…).
                foreach (string host in _manualHosts)
                {
                    try
                    {
                        IPEndPoint endPoint = ParseEndPoint(host);
                        if (endPoint == null)
                        {
                            continue;
                        }

                        bool alreadyTrying = _client.ConnectedPeerList.Any(p => p.Equals(endPoint) && p.ConnectionState != ConnectionState.Disconnected);
                        if (!alreadyTrying)
                        {
                            _client.Connect(endPoint, DatagramPayload.Version4);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Connexion à {host} impossible : {ex.Message}");
                    }
                }
            }
        }

        private IPEndPoint ParseEndPoint(string host)
        {
            string address = host;
            int port = _port;
            int colon = host.LastIndexOf(':');
            if (colon > 0 && int.TryParse(host[(colon + 1)..], out int explicitPort))
            {
                address = host[..colon];
                port = explicitPort;
            }

            if (!IPAddress.TryParse(address, out IPAddress ip))
            {
                ip = Dns.GetHostAddresses(address).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            }

            return ip == null ? null : new IPEndPoint(ip, port);
        }

        private void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Réponse du serveur à notre découverte. Le serveur répond par broadcast : on accepte les deux types.
            string message;
            try
            {
                message = reader.GetString();
            }
            catch (Exception)
            {
                return;
            }

            if (message != DatagramPayload.Version4)
            {
                return;
            }

            if (_client.ConnectedPeerList.Any(p => p.Equals(remoteEndPoint)))
            {
                return;
            }

            Log($"PC de jeu trouvé automatiquement : {remoteEndPoint.Address}:{remoteEndPoint.Port}");
            _client.Connect(remoteEndPoint, DatagramPayload.Version4);
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            DatagramPayload payload;
            try
            {
                Interlocked.Add(ref _bytes, reader.UserDataSize);
                Capture?.Add(reader.RawData, reader.UserDataOffset, reader.UserDataSize);

                // Identique au client Windows d'origine : tout le contenu utilisateur du paquet est le protobuf.
                using var stream = new MemoryStream(reader.RawData, reader.UserDataOffset, reader.UserDataSize);
                payload = Serializer.Deserialize<DatagramPayload>(stream);
            }
            catch (Exception ex)
            {
                Log("Paquet illisible : " + ex.Message);
                return;
            }
            finally
            {
                reader.Recycle();
            }

            if (!_unpackers.TryGetValue(peer, out IDatagramPayloadUnPacker unpacker))
            {
                unpacker = new DatagramPayloadUnPacker();
                _unpackers[peer] = unpacker;
            }

            SimulatorDataSet dataSet;
            try
            {
                dataSet = unpacker.UnpackDatagramPayload(payload);
            }
            catch (Exception ex)
            {
                Log("Paquet impossible à décoder : " + ex.Message);
                return;
            }

            _lastPacketUtc = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(dataSet?.Source))
            {
                _lastGame = dataSet.Source;
            }

            if (payload.PayloadKind != DatagramPayloadKind.HearthBeat && dataSet?.DriversInfo?.Length > 0)
            {
                _lastDataUtc = _lastPacketUtc;
            }

            try
            {
                DataReceived?.Invoke(dataSet, payload.PayloadKind);
            }
            catch (Exception ex)
            {
                // Une donnée inattendue d'un jeu ne doit jamais arrêter la réception : on la signale et on continue.
                Log($"Donnée non traitée ({ex.GetType().Name} : {ex.Message}) {ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}");
            }
        }

        private void Log(string message) => LogMessage?.Invoke(message);

        private static List<string> GetLocalAddresses()
        {
            try
            {
                return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
                    .Select(a => a.Address.ToString())
                    .Distinct()
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }

    /// <summary>Décode les paquets d'un enregistrement, dans l'ordre (le dépaquetage dépend des paquets précédents).</summary>
    public sealed class PayloadDecoder
    {
        private readonly DatagramPayloadUnPacker _unpacker = new();

        public (SimulatorDataSet Data, DatagramPayloadKind Kind) Decode(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            DatagramPayload payload = Serializer.Deserialize<DatagramPayload>(stream);
            return (_unpacker.UnpackDatagramPayload(payload), payload.PayloadKind);
        }
    }

    public sealed class ReceiverStatus
    {
        public int Port { get; set; }

        public bool IsConnected { get; set; }

        public List<string> Peers { get; set; } = new();

        public List<string> ManualHosts { get; set; } = new();

        public double? SecondsSinceLastPacket { get; set; }

        /// <summary>Dernières vraies données de course (pas seulement des signes de vie du collecteur).</summary>
        public double? SecondsSinceLastData { get; set; }

        /// <summary>Jeu annoncé par le collecteur.</summary>
        public string Game { get; set; }

        public double KbPerSecond { get; set; }

        public List<string> LocalAddresses { get; set; } = new();
    }
}

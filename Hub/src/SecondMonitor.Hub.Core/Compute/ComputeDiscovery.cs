namespace SecondMonitor.Hub.Core.Compute
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Serveur de calcul vu sur le réseau (dernière annonce reçue).</summary>
    public sealed class DiscoveredServer
    {
        public ComputeAnnouncement Announcement { get; set; }

        /// <summary>Adresse à utiliser : la dernière vue sur le réseau local, sinon celle de cet ordinateur (serveur sur la même machine).</summary>
        public string Address { get; set; }

        /// <summary>Toutes les adresses d'où ses annonces sont arrivées récemment (ordinateur relié en Wi-Fi et en câble…).</summary>
        public Dictionary<string, DateTime> Addresses { get; } = new(StringComparer.OrdinalIgnoreCase);

        public DateTime LastSeenUtc { get; set; }

        /// <summary>Adresse du relais vers son Ollama, telle que l'enregistre la machine reliée.</summary>
        public string BaseUrl => ComputeDiscovery.RelayUrl(Address, Announcement.Port);

        public IReadOnlyList<string> RecentAddresses(DateTime nowUtc, TimeSpan maxAge) =>
            Addresses.Where(a => nowUtc - a.Value <= maxAge).Select(a => a.Key).ToList();
    }

    /// <summary>
    /// Écoute les annonces des serveurs de calcul (<see cref="ComputeAnnouncement"/>) sur le port UDP de découverte.
    /// Sur l'ordinateur qui affiche la course (« ordinateur 2 ») : la carte « Machines IA » propose de les relier.
    /// </summary>
    public sealed class ComputeDiscovery : IDisposable
    {
        /// <summary>Un serveur qui ne s'annonce plus depuis ce délai n'est plus proposé.</summary>
        public static readonly TimeSpan Expiry = TimeSpan.FromSeconds(12);

        private readonly object _lock = new();
        private readonly Dictionary<string, DiscoveredServer> _servers = new(StringComparer.Ordinal);
        private UdpClient _udp;
        private CancellationTokenSource _cts;
        private Task _loop;

        public ComputeDiscovery(int port)
        {
            Port = port;
        }

        public int Port { get; }

        /// <summary>Raison pour laquelle l'écoute n'a pas pu démarrer (port pris, réseau refusé…).</summary>
        public string Error { get; private set; }

        /// <summary>Annonce valide reçue (appelé hors verrou).</summary>
        public event Action<DiscoveredServer> Seen;

        public void Start()
        {
            lock (_lock)
            {
                if (_udp != null)
                {
                    return;
                }

                try
                {
                    var udp = new UdpClient(AddressFamily.InterNetwork);
                    // Plusieurs programmes peuvent écouter le même port (autre SecondMonitor sur cet ordinateur, essais).
                    udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udp.EnableBroadcast = true;
                    udp.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
                    _udp = udp;
                    Error = null;
                }
                catch (Exception ex)
                {
                    Error = $"Écoute des serveurs de calcul impossible sur le port UDP {Port} : {ex.Message}";
                    return;
                }

                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;
                UdpClient client = _udp;
                _loop = Task.Run(() => LoopAsync(client, token));
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _udp?.Dispose();
                _udp = null;
            }

            try
            {
                _loop?.Wait(1000);
            }
            catch (AggregateException)
            {
            }
        }

        /// <summary>Serveurs entendus récemment, du plus récent au plus ancien.</summary>
        public IReadOnlyList<DiscoveredServer> Servers(DateTime? nowUtc = null)
        {
            DateTime now = nowUtc ?? DateTime.UtcNow;
            lock (_lock)
            {
                return _servers.Values.Where(s => now - s.LastSeenUtc <= Expiry).OrderByDescending(s => s.LastSeenUtc).Select(Copy).ToList();
            }
        }

        /// <summary>
        /// Prend en compte un datagramme reçu de <paramref name="from"/>. Faux s'il ne s'agit pas d'une annonce.
        /// Une annonce arrivée par cet ordinateur lui-même (127.0.0.1) ne remplace pas une adresse du réseau vue il y a peu.
        /// </summary>
        public bool Accept(byte[] data, int length, IPEndPoint from, DateTime nowUtc)
        {
            if (from == null || !ComputeAnnouncement.TryParse(data, length, out ComputeAnnouncement announcement))
            {
                return false;
            }

            IPAddress ip = from.Address.IsIPv4MappedToIPv6 ? from.Address.MapToIPv4() : from.Address;
            string address = ip.ToString();
            DiscoveredServer seen;
            lock (_lock)
            {
                if (!_servers.TryGetValue(announcement.Id, out DiscoveredServer server))
                {
                    server = new DiscoveredServer();
                    _servers[announcement.Id] = server;
                }

                server.Announcement = announcement;
                server.Addresses[address] = nowUtc;
                foreach (string old in server.Addresses.Where(a => nowUtc - a.Value > TimeSpan.FromMinutes(2)).Select(a => a.Key).ToList())
                {
                    server.Addresses.Remove(old);
                }

                bool keepNetworkAddress = IPAddress.IsLoopback(ip) && server.Address != null
                    && IPAddress.TryParse(server.Address, out IPAddress current) && !IPAddress.IsLoopback(current)
                    && server.Addresses.TryGetValue(server.Address, out DateTime currentSeen) && nowUtc - currentSeen <= Expiry;
                if (!keepNetworkAddress)
                {
                    server.Address = address;
                }

                server.LastSeenUtc = nowUtc;
                seen = Copy(server);
            }

            try
            {
                Seen?.Invoke(seen);
            }
            catch (Exception)
            {
                // Un abonné en erreur ne doit pas arrêter l'écoute.
            }

            return true;
        }

        /// <summary>Adresse du relais d'un serveur de calcul (« http://192.168.1.40:8686/calcul »).</summary>
        public static string RelayUrl(string address, int port)
        {
            string host = IPAddress.TryParse(address, out IPAddress ip) && ip.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{address}]" : address;
            return $"http://{host}:{port}/calcul";
        }

        private static DiscoveredServer Copy(DiscoveredServer s)
        {
            var copy = new DiscoveredServer { Announcement = s.Announcement, Address = s.Address, LastSeenUtc = s.LastSeenUtc };
            foreach (KeyValuePair<string, DateTime> a in s.Addresses)
            {
                copy.Addresses[a.Key] = a.Value;
            }

            return copy;
        }

        private async Task LoopAsync(UdpClient udp, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await udp.ReceiveAsync(token).ConfigureAwait(false);
                    Accept(result.Buffer, result.Buffer.Length, result.RemoteEndPoint, DateTime.UtcNow);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    // Windows signale parfois une erreur sur un datagramme perdu : on continue d'écouter.
                    await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>Envoi des annonces : en diffusion sur chaque réseau de l'ordinateur, et à l'ordinateur lui-même.</summary>
    public static class ComputeAnnouncer
    {
        /// <summary>Envoie l'annonce ; renvoie le nombre de destinations atteintes.</summary>
        public static int Send(UdpClient udp, byte[] payload, int port)
        {
            int sent = 0;
            foreach (IPAddress target in Targets())
            {
                try
                {
                    udp.Send(payload, payload.Length, new IPEndPoint(target, port));
                    sent++;
                }
                catch (SocketException)
                {
                    // Réseau sans diffusion (VPN, interface tombée) : les autres destinations suffisent.
                }
            }

            return sent;
        }

        /// <summary>
        /// Adresses de diffusion de chaque réseau actif (192.168.1.255…), plus 255.255.255.255 et 127.0.0.1.
        /// La diffusion dirigée passe aussi quand l'ordinateur a plusieurs cartes réseau (PC Windows avec WSL ou Hyper-V).
        /// </summary>
        public static IReadOnlyList<IPAddress> Targets()
        {
            var targets = new List<IPAddress>();
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation a in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (a.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(a.Address) || a.IPv4Mask == null)
                        {
                            continue;
                        }

                        byte[] ip = a.Address.GetAddressBytes();
                        byte[] mask = a.IPv4Mask.GetAddressBytes();
                        if (mask.All(b => b == 255) || mask.All(b => b == 0))
                        {
                            continue; // liaison point à point (VPN) : pas de diffusion
                        }

                        var broadcast = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            broadcast[i] = (byte)(ip[i] | ~mask[i]);
                        }

                        targets.Add(new IPAddress(broadcast));
                    }
                }
            }
            catch (Exception)
            {
                // Liste des cartes indisponible : la diffusion générale suffit.
            }

            targets.Add(IPAddress.Broadcast);
            targets.Add(IPAddress.Loopback);
            return targets.Distinct().ToList();
        }
    }
}

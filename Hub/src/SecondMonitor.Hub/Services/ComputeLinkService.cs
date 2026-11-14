namespace SecondMonitor.Hub.Services;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using SecondMonitor.Hub.Core.Compute;

/// <summary>
/// Côté ordinateur qui affiche la course (« ordinateur 2 ») : découvre les serveurs de calcul du réseau local,
/// se relie à l'un d'eux avec le code affiché sur son écran (il devient une machine de « Machines IA », avec son jeton),
/// et lui fait signe toutes les 20 secondes (le serveur affiche ainsi « relié à … »).
/// Si un serveur relié change d'adresse (box qui redistribue les adresses), son adresse est mise à jour d'elle-même.
/// </summary>
public sealed class ComputeLinkService : BackgroundService
{
    private static readonly TimeSpan HeartbeatEvery = TimeSpan.FromSeconds(20);
    private readonly SettingsStore _settings;
    private readonly IHttpClientFactory _http;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, LinkState> _links = new(StringComparer.OrdinalIgnoreCase);
    private ComputeDiscovery? _discovery;
    private DateTime _discoveryStartedUtc;

    public ComputeLinkService(SettingsStore settings, IHttpClientFactory http)
    {
        _settings = settings;
        _http = http;
    }

    /// <summary>Écoute des annonces en erreur (port UDP pris…), sinon null.</summary>
    public string? DiscoveryError
    {
        get { lock (_lock) { return _discovery?.Error; } }
    }

    /// <summary>Serveurs de calcul entendus récemment.</summary>
    public IReadOnlyList<DiscoveredServer> Discovered()
    {
        lock (_lock)
        {
            return _discovery?.Servers() ?? (IReadOnlyList<DiscoveredServer>)Array.Empty<DiscoveredServer>();
        }
    }

    /// <summary>Dernier signe de vie de chaque machine reliée (par nom de machine).</summary>
    public IReadOnlyDictionary<string, LinkState> Links => _links;

    /// <summary>
    /// Se relie au serveur de calcul <paramref name="address"/>:<paramref name="port"/> avec le code affiché sur son écran.
    /// La machine est ajoutée (ou mise à jour) dans « Machines IA » ; ses modèles s'écrivent ensuite « modèle@nom ».
    /// </summary>
    public async Task<PairOutcome> PairAsync(string address, int port, string code, CancellationToken ct)
    {
        string baseUrl = ComputeDiscovery.RelayUrl(address, port);
        HttpResponseMessage resp;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            resp = await _http.CreateClient("calcul").PostAsJsonAsync($"{baseUrl}/appairer", new
            {
                code,
                id = _settings.Current.Calcul.Id,
                nom = ComputeAnnouncement.CleanName(Environment.MachineName),
            }, cts.Token);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            return PairOutcome.Fail($"Serveur de calcul injoignable à {address}:{port} ({(ex is OperationCanceledException ? "pas de réponse" : ex.Message)}). Est-il allumé, en mode « serveur de calcul », et le pare-feu laisse-t-il passer SecondMonitor ?");
        }

        using (resp)
        {
            JsonNode? json = null;
            try
            {
                json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
            }
            catch (Exception)
            {
                // Réponse illisible : ce n'est pas un serveur de calcul SecondMonitor.
            }

            if (!resp.IsSuccessStatusCode)
            {
                string? error = json?["error"]?.GetValue<string>();
                return PairOutcome.Fail(resp.StatusCode == HttpStatusCode.NotFound
                    ? $"Pas de serveur de calcul SecondMonitor à {address}:{port} (SecondMonitor à jour et en mode « serveur de calcul » sur l'autre ordinateur ?)."
                    : error ?? $"Le serveur a répondu {(int)resp.StatusCode}.");
            }

            string? token = json?["jeton"]?.GetValue<string>();
            string serverId = json?["id"]?.GetValue<string>() ?? "";
            string serverName = ComputeAnnouncement.CleanName(json?["nom"]?.GetValue<string>());
            if (string.IsNullOrEmpty(token) || !ComputeAnnouncement.IsValidId(serverId))
            {
                return PairOutcome.Fail("Réponse inattendue du serveur de calcul.");
            }

            string nodeName = "";
            _settings.Update(s =>
            {
                AiSettings ai = s.Ai;
                AiNode? node = ai.Nodes.FirstOrDefault(n => n.ServerId == serverId)
                    ?? ai.Nodes.FirstOrDefault(n => (n.Url ?? "").TrimEnd('/').Equals(baseUrl, StringComparison.OrdinalIgnoreCase));
                if (node == null)
                {
                    node = new AiNode { Name = UniqueName(ai, serverName.Length > 0 ? serverName : "Calcul") };
                    ai.Nodes.Add(node);
                }

                node.Url = baseUrl;
                node.Token = token;
                node.ServerId = serverId;
                nodeName = node.Name;
            });
            _links[nodeName] = new LinkState { Ok = true, CheckedUtc = DateTime.UtcNow };
            HubJournal.Write(_settings.DataDirectory, "serveur de calcul", $"Relié à « {serverName} » ({baseUrl}) sous le nom « {nodeName} »");
            return new PairOutcome(true, nodeName, null);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime lastHeartbeat = DateTime.MinValue;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                UpdateListener();
                if (DateTime.UtcNow - lastHeartbeat >= HeartbeatEvery)
                {
                    lastHeartbeat = DateTime.UtcNow;
                    await HeartbeatAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_lock)
            {
                _discovery?.Dispose();
                _discovery = null;
            }
        }
    }

    /// <summary>Écoute des annonces : pas en mode serveur de calcul ; redémarrée si le port change.</summary>
    private void UpdateListener()
    {
        HubSettings s = _settings.Current;
        lock (_lock)
        {
            bool wanted = !s.IsComputeServer;
            bool retry = _discovery?.Error != null && DateTime.UtcNow - _discoveryStartedUtc > TimeSpan.FromSeconds(15);
            if (_discovery != null && (!wanted || retry || _discovery.Port != s.Calcul.DiscoveryPort))
            {
                _discovery.Dispose();
                _discovery = null;
            }

            if (wanted && _discovery == null)
            {
                _discovery = new ComputeDiscovery(s.Calcul.DiscoveryPort);
                _discovery.Seen += OnSeen;
                _discovery.Start();
                _discoveryStartedUtc = DateTime.UtcNow;
            }
        }
    }

    /// <summary>Un serveur relié s'annonce depuis une autre adresse que celle enregistrée : on suit.</summary>
    private void OnSeen(DiscoveredServer server)
    {
        AiNode? node = _settings.Current.Ai.Nodes.FirstOrDefault(n => n.ServerId == server.Announcement.Id && !string.IsNullOrEmpty(n.Token));
        if (node == null || !Uri.TryCreate(node.Url, UriKind.Absolute, out Uri? current))
        {
            return;
        }

        IReadOnlyList<string> recent = server.RecentAddresses(DateTime.UtcNow, TimeSpan.FromSeconds(30));
        bool known = current.Port == server.Announcement.Port && recent.Any(a => a.Equals(current.Host.Trim('[', ']'), StringComparison.OrdinalIgnoreCase));
        if (known)
        {
            return;
        }

        string url = server.BaseUrl;
        _settings.Update(s =>
        {
            AiNode? n = s.Ai.Nodes.FirstOrDefault(x => x.ServerId == server.Announcement.Id);
            if (n != null)
            {
                n.Url = url;
            }
        });
        HubJournal.Write(_settings.DataDirectory, "serveur de calcul", $"« {node.Name} » a changé d'adresse : {url}");
    }

    /// <summary>Signe de vie aux serveurs reliés (le serveur affiche « relié à … » ; ici : jeton accepté ou refusé).</summary>
    private async Task HeartbeatAsync(CancellationToken ct)
    {
        List<AiNode> nodes = _settings.Current.Ai.Nodes.Where(n => !string.IsNullOrEmpty(n.Token)).ToList();
        foreach (string gone in _links.Keys.Where(k => !nodes.Any(n => n.Name.Equals(k, StringComparison.OrdinalIgnoreCase))).ToList())
        {
            _links.TryRemove(gone, out _);
        }

        await Task.WhenAll(nodes.Select(async node =>
        {
            var state = new LinkState { CheckedUtc = DateTime.UtcNow };
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(4));
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{node.Url.TrimEnd('/')}/ping");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", node.Token);
                using HttpResponseMessage resp = await _http.CreateClient("calcul").SendAsync(request, cts.Token);
                state.Ok = resp.IsSuccessStatusCode;
                state.Refused = resp.StatusCode == HttpStatusCode.Unauthorized;
                state.Error = state.Ok ? null : state.Refused ? "jeton refusé : reliez de nouveau cette machine" : $"le serveur a répondu {(int)resp.StatusCode}";
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                state.Error = ex is OperationCanceledException ? "pas de réponse" : ex.Message;
            }

            _links[node.Name] = state;
        }));
    }

    /// <summary>Nom de machine libre (« PC-RTX », sinon « PC-RTX 2 »…), différent de celui de cet ordinateur.</summary>
    private static string UniqueName(AiSettings ai, string wanted)
    {
        string name = wanted;
        for (int i = 2; ai.Nodes.Any(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) || name.Equals(ai.LocalNodeName, StringComparison.OrdinalIgnoreCase); i++)
        {
            name = $"{wanted} {i}";
        }

        return name;
    }

    public sealed class LinkState
    {
        public bool Ok { get; set; }

        public bool Refused { get; set; }

        public string? Error { get; set; }

        public DateTime CheckedUtc { get; set; }
    }

    public sealed record PairOutcome(bool Ok, string? Node, string? Error)
    {
        public static PairOutcome Fail(string error) => new(false, null, error);
    }
}

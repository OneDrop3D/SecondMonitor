namespace SecondMonitor.Hub.Services;

using System.Net.Sockets;
using System.Text.Json.Nodes;
using SecondMonitor.Hub.Core.Compute;

/// <summary>
/// Serveur de calcul (« ordinateur 3 ») : s'annonce sur le réseau local toutes les 2 secondes et suit l'état de l'Ollama
/// de cet ordinateur (modèles installés). Le relais lui-même est dans <see cref="ComputeServerApi"/>.
/// Ne fait rien tant que cet ordinateur n'est pas en mode « serveur de calcul ».
/// </summary>
public sealed class ComputeServerService : BackgroundService
{
    private static readonly TimeSpan AnnounceEvery = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ModelsEvery = TimeSpan.FromSeconds(15);
    private readonly SettingsStore _settings;
    private readonly IHttpClientFactory _http;
    private readonly object _lock = new();
    private List<LocalModel> _models = new();
    private bool _ollamaOk;
    private string? _ollamaError;
    private DateTime _modelsUtc = DateTime.MinValue;
    private DateTime _announcedUtc = DateTime.MinValue;
    private int _announceTargets;

    public ComputeServerService(SettingsStore settings, IHttpClientFactory http)
    {
        _settings = settings;
        _http = http;
    }

    /// <summary>Nom annoncé : celui choisi sur le serveur, sinon le nom de l'ordinateur.</summary>
    public string ServerName
    {
        get
        {
            string chosen = ComputeAnnouncement.CleanName(_settings.Current.Calcul.Name);
            if (chosen.Length > 0)
            {
                return chosen;
            }

            string machine = ComputeAnnouncement.CleanName(Environment.MachineName);
            return machine.Length > 0 ? machine : "Serveur de calcul";
        }
    }

    public string ServerId => _settings.Current.Calcul.Id;

    public (bool Ok, string? Error, IReadOnlyList<LocalModel> Models, DateTime CheckedUtc, DateTime AnnouncedUtc, int Targets) State()
    {
        lock (_lock)
        {
            return (_ollamaOk, _ollamaError, _models, _modelsUtc, _announcedUtc, _announceTargets);
        }
    }

    public ComputeAnnouncement Announcement()
    {
        lock (_lock)
        {
            return new ComputeAnnouncement { Id = ServerId, Name = ServerName, Port = _settings.Current.WebPort, Models = _models.Count, OllamaOk = _ollamaOk };
        }
    }

    /// <summary>Relit la liste des modèles de l'Ollama de cet ordinateur (« /api/tags »).</summary>
    public async Task RefreshModelsAsync(CancellationToken ct)
    {
        string url = OllamaBase(_settings.Current.Ai.OllamaUrl);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            HttpClient client = _http.CreateClient("calcul");
            JsonNode? tags = await client.GetFromJsonAsync<JsonNode>($"{url}/api/tags", cts.Token);
            var models = (tags?["models"]?.AsArray() ?? new JsonArray())
                .Select(m => new LocalModel(m?["name"]?.GetValue<string>() ?? "", m?["size"]?.GetValue<long>() ?? 0))
                .Where(m => m.Name.Length > 0)
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            lock (_lock)
            {
                _models = models;
                _ollamaOk = true;
                _ollamaError = null;
                _modelsUtc = DateTime.UtcNow;
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            lock (_lock)
            {
                _models = new List<LocalModel>();
                _ollamaOk = false;
                _ollamaError = ex is OperationCanceledException ? "pas de réponse" : ex.Message;
                _modelsUtc = DateTime.UtcNow;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_settings.Current.IsComputeServer)
                {
                    if (DateTime.UtcNow - _modelsUtc > ModelsEvery)
                    {
                        await RefreshModelsAsync(stoppingToken);
                    }

                    int sent = ComputeAnnouncer.Send(udp, Announcement().Encode(), _settings.Current.Calcul.DiscoveryPort);
                    lock (_lock)
                    {
                        _announcedUtc = DateTime.UtcNow;
                        _announceTargets = sent;
                    }
                }

                await Task.Delay(AnnounceEvery, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Réseau momentanément indisponible : on réessaie au tour suivant.
                await Task.Delay(AnnounceEvery, CancellationToken.None);
            }
        }
    }

    /// <summary>Adresse de l'Ollama de cet ordinateur (« 127.0.0.1:11434 » → « http://127.0.0.1:11434 »).</summary>
    public static string OllamaBase(string url)
    {
        url = (url ?? string.Empty).Trim().TrimEnd('/');
        if (url.Length == 0)
        {
            url = "http://127.0.0.1:11434";
        }

        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = "http://" + url;
        }

        var uri = new Uri(url);
        return uri.IsDefaultPort && !url.Contains(":" + uri.Port) ? $"{uri.Scheme}://{uri.Host}:11434" : url;
    }

    public sealed record LocalModel(string Name, long SizeBytes);
}

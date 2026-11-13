namespace SecondMonitor.Hub.Services;

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Accès à Ollama, sur ce Mac et sur d'autres machines du réseau (Raspberry Pi, PC…) : modèles et capacités,
/// conversation, transcription, synthèse vocale. Un modèle s'écrit « nom » (ce Mac) ou « nom@machine ».
/// </summary>
public sealed class OllamaClient
{
    private readonly HttpClient _http;
    private readonly SettingsStore _settings;

    public OllamaClient(HttpClient http, SettingsStore settings)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(120);
        _settings = settings;
    }

    /// <summary>Machine et nom réel d'un modèle « nom@machine ».</summary>
    public (string Url, string Model, string Node) Resolve(string spec)
    {
        AiSettings ai = _settings.Current.Ai;
        int at = spec.LastIndexOf('@');
        if (at > 0)
        {
            string nodeName = spec[(at + 1)..];
            AiNode? node = ai.Nodes.FirstOrDefault(n => n.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
            if (node != null)
            {
                return (Normalize(node.Url), spec[..at], node.Name);
            }
        }

        return (Normalize(ai.OllamaUrl), spec, ai.LocalNodeName);
    }

    /// <summary>Toutes les machines (ce Mac d'abord) et leurs modèles ; une machine injoignable n'empêche pas les autres.</summary>
    public async Task<(List<OllamaModel> Models, List<NodeStatus> Nodes)> ListAllAsync(CancellationToken ct)
    {
        AiSettings ai = _settings.Current.Ai;
        var nodes = new List<(string Name, string Url, bool Local)> { (ai.LocalNodeName, Normalize(ai.OllamaUrl), true) };
        nodes.AddRange(ai.Nodes.Where(n => !string.IsNullOrWhiteSpace(n.Url)).Select(n => (n.Name, Normalize(n.Url), false)));
        var tasks = nodes.Select(async n =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(n.Local ? 10 : 4));
                List<OllamaModel> models = await ListModelsAsync(n.Url, cts.Token);
                foreach (OllamaModel m in models)
                {
                    m.Node = n.Name;
                    m.Id = n.Local ? m.Name : $"{m.Name}@{n.Name}";
                }

                return (Models: models, Status: new NodeStatus { Name = n.Name, Url = n.Url, Local = n.Local, Ok = true, ModelCount = models.Count, Milliseconds = (int)sw.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                return (Models: new List<OllamaModel>(), Status: new NodeStatus { Name = n.Name, Url = n.Url, Local = n.Local, Ok = false, Error = ex is OperationCanceledException ? "pas de réponse" : ex.Message, Milliseconds = (int)sw.ElapsedMilliseconds });
            }
        }).ToList();
        var results = await Task.WhenAll(tasks);
        return (results.SelectMany(r => r.Models).ToList(), results.Select(r => r.Status).ToList());
    }

    /// <summary>Modèles de ce Mac uniquement (compatibilité).</summary>
    public Task<List<OllamaModel>> ListModelsAsync(CancellationToken ct) => ListModelsAsync(Normalize(_settings.Current.Ai.OllamaUrl), ct);

    public async Task<List<OllamaModel>> ListModelsAsync(string baseUrl, CancellationToken ct)
    {
        JsonNode? tags = await _http.GetFromJsonAsync<JsonNode>($"{baseUrl}/api/tags", ct);
        var result = new List<OllamaModel>();
        foreach (JsonNode? m in tags?["models"]?.AsArray() ?? new JsonArray())
        {
            string? name = m?["name"]?.GetValue<string>();
            if (name == null)
            {
                continue;
            }

            var model = new OllamaModel { Name = name, Id = name, SizeBytes = m?["size"]?.GetValue<long>() ?? 0 };
            try
            {
                using var resp = await _http.PostAsJsonAsync($"{baseUrl}/api/show", new { model = name }, ct);
                JsonNode? show = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
                model.Capabilities = show?["capabilities"]?.AsArray().Select(c => c!.GetValue<string>()).ToList() ?? new();
                model.Voices = show?["voices"]?.AsArray().Select(c => c!.GetValue<string>()).ToList() ?? new();
            }
            catch (Exception)
            {
                // Modèle sans fiche détaillée : on garde au moins son nom.
            }

            result.Add(model);
        }

        return result;
    }

    public async Task<string> ChatAsync(string spec, IEnumerable<object> messages, CancellationToken ct, int maxTokens = 160)
    {
        var (url, model, _) = Resolve(spec);
        var body = new
        {
            model,
            stream = false,
            keep_alive = "30m",
            options = new { temperature = 0.2, num_predict = maxTokens },
            messages,
        };
        using var resp = await _http.PostAsJsonAsync($"{url}/api/chat", body, ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        return json?["message"]?["content"]?.GetValue<string>()?.Trim() ?? string.Empty;
    }

    /// <summary>Transcription d'un fichier audio (WAV conseillé) — route compatible OpenAI d'Ollama.</summary>
    public async Task<string> TranscribeAsync(string spec, byte[] audio, string fileName, string language, CancellationToken ct)
    {
        var (url, model, _) = Resolve(spec);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(model), "model");
        if (!string.IsNullOrEmpty(language))
        {
            form.Add(new StringContent(language), "language");
        }

        var file = new ByteArrayContent(audio);
        file.Headers.ContentType = new MediaTypeHeaderValue(fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? "audio/wav" : "application/octet-stream");
        form.Add(file, "file", fileName);
        using var resp = await _http.PostAsync($"{url}/v1/audio/transcriptions", form, ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        return json?["text"]?.GetValue<string>()?.Trim() ?? string.Empty;
    }

    /// <summary>Synthèse vocale → WAV.</summary>
    public async Task<byte[]> SpeakAsync(string spec, string text, string voice, CancellationToken ct)
    {
        var (url, model, _) = Resolve(spec);
        var body = new { model, input = text, voice = string.IsNullOrEmpty(voice) ? "default" : voice, response_format = "wav" };
        using var resp = await _http.PostAsJsonAsync($"{url}/v1/audio/speech", body, ct);
        await EnsureOk(resp, ct);
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    private static string Normalize(string url)
    {
        url = (url ?? string.Empty).Trim().TrimEnd('/');
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = "http://" + url;
        }

        // « raspberrypi.local » sans port → port par défaut d'Ollama.
        var uri = new Uri(url);
        return uri.IsDefaultPort && !url.Contains(":" + uri.Port) ? $"{uri.Scheme}://{uri.Host}:11434" : url;
    }

    private static async Task EnsureOk(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode)
        {
            return;
        }

        string body = await resp.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"Ollama a répondu {(int)resp.StatusCode} : {body}");
    }
}

public sealed class OllamaModel
{
    /// <summary>Identifiant à enregistrer dans les réglages (« nom » ou « nom@machine »).</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Node { get; set; } = "";

    public long SizeBytes { get; set; }

    public List<string> Capabilities { get; set; } = new();

    public List<string> Voices { get; set; } = new();
}

public sealed class NodeStatus
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public bool Local { get; set; }

    public bool Ok { get; set; }

    public string? Error { get; set; }

    public int ModelCount { get; set; }

    public int Milliseconds { get; set; }
}

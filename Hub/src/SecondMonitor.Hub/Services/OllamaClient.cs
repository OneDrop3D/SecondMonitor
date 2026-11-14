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
        using var tagsResp = await SendAsync(HttpMethod.Get, $"{baseUrl}/api/tags", null, ct);
        await EnsureOk(tagsResp, ct);
        JsonNode? tags = await tagsResp.Content.ReadFromJsonAsync<JsonNode>(ct);
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
                using var resp = await SendAsync(HttpMethod.Post, $"{baseUrl}/api/show", JsonContent.Create(new { model = name }), ct);
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

    /// <summary>Capacité « thinking » par modèle (url|nom), retenue pour toute la vie de l'application (le client est recréé à chaque usage).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> ThinkingSupport = new();

    /// <summary>Le modèle a-t-il un mode « thinking » (raisonnement avant de répondre) ? Lu une fois par /api/show.</summary>
    public async Task<bool> SupportsThinkingAsync(string spec, CancellationToken ct)
    {
        var (url, model, _) = Resolve(spec);
        string key = $"{url}|{model}";
        if (ThinkingSupport.TryGetValue(key, out bool known))
        {
            return known;
        }

        try
        {
            using var resp = await SendAsync(HttpMethod.Post, $"{url}/api/show", JsonContent.Create(new { model }), ct);
            JsonNode? show = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
            bool thinks = show?["capabilities"]?.AsArray().Any(c => c?.GetValue<string>() == "thinking") == true;
            ThinkingSupport[key] = thinks;
            return thinks;
        }
        catch (Exception)
        {
            return false; // fiche indisponible : on n'envoie pas « think » (Ollama refuserait la requête si le modèle ne l'a pas)
        }
    }

    /// <summary>
    /// Conversation. <paramref name="think"/> : mode « thinking » pour les modèles qui l'ont (raisonnement plus long, réponse plus réfléchie) ;
    /// pour ces modèles, il est toujours envoyé (vrai ou faux), sinon certains réfléchissent d'office. Ignoré pour les autres.
    /// </summary>
    public async Task<string> ChatAsync(string spec, IEnumerable<object> messages, CancellationToken ct, int maxTokens = 160, bool think = false)
    {
        var (url, model, _) = Resolve(spec);
        bool thinks = await SupportsThinkingAsync(spec, ct);
        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["stream"] = false,
            ["keep_alive"] = "30m",
            // Le raisonnement compte dans la limite : on la relève quand il est demandé, pour qu'il reste de la place pour la réponse.
            ["options"] = new { temperature = 0.2, num_predict = thinks && think ? Math.Max(maxTokens, 2048) : maxTokens },
            ["messages"] = messages,
        };
        if (thinks)
        {
            body["think"] = think;
        }

        using var resp = await SendAsync(HttpMethod.Post, $"{url}/api/chat", JsonContent.Create(body), ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        string content = json?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
        // Anciennes versions d'Ollama : le raisonnement arrive dans la réponse, entre balises.
        content = System.Text.RegularExpressions.Regex.Replace(content, @"<think>[\s\S]*?</think>", string.Empty);
        return content.Trim();
    }

    /// <summary>
    /// Transcription d'un fichier audio (WAV conseillé) — route compatible OpenAI d'Ollama. <paramref name="prompt"/> : mots à
    /// reconnaître (vocabulaire de piste), pour les modèles qui l'acceptent.
    /// </summary>
    public async Task<string> TranscribeAsync(string spec, byte[] audio, string fileName, string language, CancellationToken ct, string? prompt = null)
    {
        var (url, model, _) = Resolve(spec);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(model), "model");
        if (!string.IsNullOrEmpty(language))
        {
            form.Add(new StringContent(language), "language");
        }

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            form.Add(new StringContent(prompt), "prompt");
        }


        var file = new ByteArrayContent(audio);
        file.Headers.ContentType = new MediaTypeHeaderValue(fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? "audio/wav" : "application/octet-stream");
        form.Add(file, "file", fileName);
        using var resp = await SendAsync(HttpMethod.Post, $"{url}/v1/audio/transcriptions", form, ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        return json?["text"]?.GetValue<string>()?.Trim() ?? string.Empty;
    }

    /// <summary>Synthèse vocale → WAV.</summary>
    public async Task<byte[]> SpeakAsync(string spec, string text, string voice, CancellationToken ct)
    {
        var (url, model, _) = Resolve(spec);
        var body = new { model, input = text, voice = string.IsNullOrEmpty(voice) ? "default" : voice, response_format = "wav" };
        using var resp = await SendAsync(HttpMethod.Post, $"{url}/v1/audio/speech", JsonContent.Create(body), ct);
        await EnsureOk(resp, ct);
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    /// <summary>
    /// Requête vers une machine. Un serveur de calcul SecondMonitor relié (carte « Machines IA ») reçoit son jeton
    /// dans l'en-tête « Authorization » ; un Ollama ouvert ne reçoit rien de plus.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        AiNode? node = _settings.Current.Ai.Nodes.FirstOrDefault(n => !string.IsNullOrEmpty(n.Token) && url.StartsWith(Normalize(n.Url) + "/", StringComparison.OrdinalIgnoreCase));
        if (node != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", node.Token);
        }

        return await _http.SendAsync(request, ct);
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

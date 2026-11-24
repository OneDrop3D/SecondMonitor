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
        if (Gateway(baseUrl) is AiNode gw)
        {
            List<OllamaModel> free = await ListGatewayAsync(gw, ct);
            return gw.Models.Count == 0 ? free : free.Where(m => gw.Models.Contains(m.Name)).ToList();
        }

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

    /// <summary>Machine de type passerelle OpenAI dont l'adresse est <paramref name="url"/>, sinon null.</summary>
    private AiNode? Gateway(string url) => _settings.Current.Ai.Nodes.FirstOrDefault(n => n.Api == "openai" && Normalize(n.Url).Equals(url, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Modèles de conversation gratuits d'une passerelle, du meilleur au moins bon.
    /// Experiential Labs : ids « …:free » classés par prix de leur version payante. NVIDIA (build.nvidia.com) : tout est gratuit, classé par taille.
    /// </summary>
    public async Task<List<OllamaModel>> ListGatewayAsync(AiNode node, CancellationToken ct)
    {
        using var resp = await SendAsync(HttpMethod.Get, $"{Normalize(node.Url)}/v1/models", null, ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        var all = (json?["data"]?.AsArray() ?? new JsonArray()).Where(m => m?["id"] != null).DistinctBy(m => m!["id"]!.GetValue<string>()).ToDictionary(m => m!["id"]!.GetValue<string>(), m => m!);
        IEnumerable<string> ids;
        if (IsNvidia(node.Url))
        {
            // shortcut: /v1/models de NVIDIA ne dit ni le type ni la qualité ; on écarte les non-conversationnels par leur nom
            // et on classe par taille lue dans le nom (« 253b », « 8x22b ») ; à revoir si NVIDIA publie ces champs.
            ids = all.Keys.Where(id => !NonChat.IsMatch(id)).OrderByDescending(SizeB).ThenBy(id => id, StringComparer.Ordinal);
        }
        else
        {
            // shortcut: le prix de la version payante (id sans « :free ») sert de mesure de qualité, faute de note ; inconnu = en dernier.
            long Price(string id) => all.TryGetValue(id[..^5], out JsonNode? paid) ? paid["pricing"]?["output_nano_usd_per_million_tokens"]?.GetValue<long?>() ?? -1 : -1;
            long Context(string id) => all[id]["context_window_tokens"]?.GetValue<long?>() ?? 0;
            ids = all.Keys.Where(id => id.EndsWith(":free", StringComparison.Ordinal)).OrderByDescending(Price).ThenByDescending(Context);
        }

        return ids.Select(id => new OllamaModel { Name = id, Id = id, Capabilities = new() { "completion" } }).ToList();
    }

    private static bool IsNvidia(string url) => url.Contains("nvidia.com", StringComparison.OrdinalIgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex NonChat =
        new(@"embed|rerank|clip|guard|reward|safety|parse|detect|pii|retriever|vila|neva|kosmos|paligemma|deplot|fuyu|cosmos|flux|sdxl|stable-diffusion|asr|tts|riva", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// <summary>Milliards de paramètres lus dans un id (« llama-3.1-405b » → 405, « mixtral-8x22b » → 176) ; 0 si absent.</summary>
    internal static double SizeB(string id)
    {
        var m = System.Text.RegularExpressions.Regex.Match(id, @"(?<![a-z\d])(?:(\d+)x)?(\d+(?:\.\d+)?)b(?![a-z])", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return !m.Success ? 0 : double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) * (m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 1);
    }

    /// <summary>Capacité « thinking » par modèle (url|nom), retenue pour toute la vie de l'application (le client est recréé à chaque usage).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> ThinkingSupport = new();

    /// <summary>Le modèle a-t-il un mode « thinking » (raisonnement avant de répondre) ? Lu une fois par /api/show.</summary>
    public async Task<bool> SupportsThinkingAsync(string spec, CancellationToken ct)
    {
        var (url, model, _) = Resolve(spec);
        if (Gateway(url) != null)
        {
            return false; // le raisonnement de la passerelle est géré par elle (reasoning_effort non envoyé)
        }

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
        if (Gateway(url) != null)
        {
            // Corps minimal : model + messages + max_tokens (pas d'échantillonnage, certains modèles le refusent → 502).
            var gw = new { model, messages, max_tokens = Math.Max(maxTokens, think ? 2048 : 512) };
            using var gResp = await SendAsync(HttpMethod.Post, $"{url}/v1/chat/completions", JsonContent.Create(gw), ct);
            await EnsureOk(gResp, ct);
            JsonNode? gJson = await gResp.Content.ReadFromJsonAsync<JsonNode>(ct);
            string text = gJson?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(text, @"<think>[\s\S]*?</think>", string.Empty).Trim(); // modèles qui raisonnent en clair (NVIDIA)
        }

        bool thinks = await SupportsThinkingAsync(spec, ct);
        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["stream"] = false,
            ["keep_alive"] = "30m",
            // Le raisonnement compte dans la limite : on la relève quand il est demandé, pour qu'il reste de la place pour la réponse.
            // num_ctx borné : le cache de contexte grossit vite et pèse sur la RAM (8192 jetons ≈ 0,7 Go avec le cache KV en q8_0).
            ["options"] = new { temperature = 0.2, num_ctx = 8192, num_predict = thinks && think ? Math.Max(maxTokens, 2048) : maxTokens },
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
        else if (_settings.Current.Ai.Nodes.Any(n => n.Api == "openai" && url.StartsWith(Normalize(n.Url) + "/", StringComparison.OrdinalIgnoreCase))
                 && Environment.GetEnvironmentVariable(IsNvidia(url) ? "NVIDIA_API_KEY" : "EXPLABS_API_KEY") is { Length: > 0 } envKey)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", envKey); // clé saisie nulle part : variable d'environnement
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

        // « raspberrypi.local » sans port → port par défaut d'Ollama (http seulement : https = passerelle distante).
        var uri = new Uri(url);
        return uri.Scheme == "http" && uri.IsDefaultPort && !url.Contains(":" + uri.Port) ? $"{uri.Scheme}://{uri.Host}:11434" : url;
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

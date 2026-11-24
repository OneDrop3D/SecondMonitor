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

    /// <summary>Fournisseurs en pause (crédit épuisé, clé refusée) : nom → fin de la pause. Statique : le client est recréé à chaque usage.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> Paused = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Dernier fournisseur qui a répondu pour chaque modèle à bascule (« modèle@fournisseur »).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> Answered = new(StringComparer.OrdinalIgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex CreditWords =
        new(@"credit|quota|insufficient|exhaust|balance|billing|payment", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// <summary>Minutes restantes de la pause d'un fournisseur (0 = disponible).</summary>
    public static int PausedMinutes(string node) =>
        Paused.TryGetValue(node, out DateTime until) && until > DateTime.UtcNow ? (int)Math.Ceiling((until - DateTime.UtcNow).TotalMinutes) : 0;

    /// <summary>Remet un fournisseur en service (nouvelle clé).</summary>
    public static void Resume(string node) => Paused.TryRemove(node, out _);

    /// <summary>Le fournisseur qui a répondu en dernier pour ce modèle à bascule, sinon null.</summary>
    public static string? AnsweredBy(string route) => Answered.GetValueOrDefault(route);

    private AiRoute? Route(string spec) => _settings.Current.Ai.Routes?.FirstOrDefault(r => r.Name.Equals(spec, StringComparison.OrdinalIgnoreCase));

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
        List<NodeStatus> statuses = results.Select(r => r.Status).ToList();
        statuses.ForEach(st => st.PausedMinutes = PausedMinutes(st.Name));
        List<OllamaModel> all = results.SelectMany(r => r.Models).ToList();
        // Les modèles à bascule se choisissent comme les autres (liste des modèles, ordre à la voix « passe sur … »).
        all.AddRange((ai.Routes ?? new()).Where(r => r.Targets.Count > 0).Select(r => new OllamaModel { Name = r.Name, Id = r.Name, Node = "bascule", Capabilities = new() { "completion" } }));
        return (all, statuses);
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

    /// <summary>Fournisseur d'API (compatible OpenAI ou Claude) dont l'adresse est <paramref name="url"/>, sinon null.</summary>
    private AiNode? Gateway(string url) => _settings.Current.Ai.Nodes.FirstOrDefault(n => n.IsGateway && Normalize(n.Url).Equals(url, StringComparison.OrdinalIgnoreCase));

    private static string GatewayUrl(AiNode node, string route) => $"{Normalize(node.Url)}{node.ApiPath}{route}";

    /// <summary>
    /// Modèles de conversation gratuits d'une passerelle, du meilleur au moins bon.
    /// Experiential Labs : ids « …:free » classés par prix de leur version payante. NVIDIA (build.nvidia.com) : tout est gratuit, classé par taille.
    /// </summary>
    public async Task<List<OllamaModel>> ListGatewayAsync(AiNode node, CancellationToken ct)
    {
        using var resp = await SendAsync(HttpMethod.Get, GatewayUrl(node, node.Api == "anthropic" ? "/models?limit=100" : "/models"), null, ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        var all = (json?["data"]?.AsArray() ?? new JsonArray()).Where(m => m?["id"] != null).DistinctBy(m => m!["id"]!.GetValue<string>()).ToDictionary(m => m!["id"]!.GetValue<string>(), m => m!);
        IEnumerable<string> ids;
        if (node.Api == "anthropic")
        {
            ids = all.Keys; // Claude : le plus récent d'abord, tel que l'API le renvoie
        }
        else if (IsNvidia(node.Url) || !all.Keys.Any(id => id.EndsWith(":free", StringComparison.Ordinal)))
        {
            // shortcut: /v1/models de NVIDIA (et des fournisseurs sans « :free ») ne dit ni le type ni la qualité ; on écarte les non-conversationnels par leur nom
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
        new(@"embed|rerank|clip|guard|reward|safety|parse|detect|pii|retriever|vila|neva|kosmos|paligemma|deplot|fuyu|cosmos|flux|sdxl|stable-diffusion|asr|tts|riva|whisper|dall-e|moderation|image|audio|realtime|transcri|banana|lyria|robotics|orpheus|veo|imagen|genie|playai", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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
        if (Route(spec) != null || Gateway(url) != null)
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
        if (Route(spec) is not AiRoute route)
        {
            return await ChatOnceAsync(spec, messages, ct, maxTokens, think);
        }

        // Modèle à bascule : du premier au dernier, en sautant les fournisseurs en pause ; seule une erreur de crédit/clé met en pause.
        messages = messages.ToList();
        // Chaîne « then » : les cibles de la route, puis celles de la suivante, etc. (une route déjà vue arrête la chaîne).
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targets = new List<string>();
        for (AiRoute? r = route; r != null && seen.Add(r.Name); r = string.IsNullOrWhiteSpace(r.Then) ? null : Route(r.Then))
        {
            targets.AddRange(r.Targets.Where(t => !targets.Contains(t, StringComparer.OrdinalIgnoreCase)));
        }

        Exception? last = null;
        for (int i = 0; i < targets.Count; i++)
        {
            string target = targets[i];
            string node = Resolve(target).Node;
            if (PausedMinutes(node) > 0)
            {
                continue;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (i < targets.Count - 1)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // un fournisseur qui traîne ne bloque pas la radio : on passe au suivant
            }

            try
            {
                string text = await ChatOnceAsync(target, messages, cts.Token, maxTokens, think);
                if (string.IsNullOrWhiteSpace(text))
                {
                    last = new InvalidOperationException($"{target} : réponse vide");
                    continue; // refus ou réflexion trop longue : on essaie le fournisseur suivant
                }

                Answered[route.Name] = target;
                return text;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                last = ex;
                PauseAfter(node, ex);
            }
        }

        throw last ?? new InvalidOperationException($"« {route.Name} » : tous les fournisseurs sont en pause (crédit épuisé). Ajoutez du crédit ou une nouvelle clé.");
    }

    /// <summary>Crédit épuisé (402, ou un message qui le dit) ou clé refusée (401) : pause de <see cref="AiNode.PauseMinutes"/> ; trop de requêtes (429) : une minute.</summary>
    private void PauseAfter(string node, Exception ex)
    {
        if (ex is not AiHttpException h)
        {
            return; // réseau, délai, 5xx, modèle inconnu : on passe au suivant sans condamner le fournisseur
        }

        bool credit = h.Status is 401 or 402 || CreditWords.IsMatch(h.Body);
        if (!credit && h.Status != 429)
        {
            return;
        }

        int configured = _settings.Current.Ai.Nodes.FirstOrDefault(n => n.Name.Equals(node, StringComparison.OrdinalIgnoreCase))?.PauseMinutes ?? 60;
        int minutes = credit ? Math.Clamp(configured, 1, 43200) : 1;
        Paused[node] = DateTime.UtcNow.AddMinutes(minutes);
    }

    /// <summary>
    /// Claude, par son API native (/messages) : le « system » est un champ à part, pas un message. Sur les modèles 5.x la réflexion est
    /// active d'office : effort « low » (réponses radio rapides) et une limite assez large pour qu'elle ne mange pas la réponse.
    /// </summary>
    private async Task<string> ChatClaudeAsync(AiNode gateway, string model, IEnumerable<object> messages, CancellationToken ct, int maxTokens, bool think)
    {
        var system = new List<string>();
        var turns = new List<object>();
        foreach (object m in messages)
        {
            JsonNode? node = System.Text.Json.JsonSerializer.SerializeToNode(m);
            string role = node?["role"]?.GetValue<string>() ?? "user", content = node?["content"]?.ToString() ?? string.Empty;
            if (role == "system")
            {
                system.Add(content);
            }
            else
            {
                turns.Add(new { role, content });
            }
        }

        var body = new Dictionary<string, object> { ["model"] = model, ["max_tokens"] = Math.Max(maxTokens, think ? 8192 : 2048), ["messages"] = turns };
        if (system.Count > 0)
        {
            body["system"] = string.Join("\n\n", system);
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(model, "^claude-(haiku|sonnet|opus|fable|mythos)-5"))
        {
            body["output_config"] = new { effort = "low" }; // shortcut: l'effort est refusé par les anciens modèles ; seule la famille 5.x le reçoit
        }

        using var resp = await SendAsync(HttpMethod.Post, GatewayUrl(gateway, "/messages"), JsonContent.Create(body), ct);
        await EnsureOk(resp, ct);
        JsonNode? json = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
        string text = string.Concat((json?["content"]?.AsArray() ?? new JsonArray()).Where(b => b?["type"]?.GetValue<string>() == "text").Select(b => b!["text"]?.GetValue<string>())).Trim();
        if (text.Length == 0 && json?["stop_reason"]?.GetValue<string>() == "refusal")
        {
            throw new InvalidOperationException("Claude a refusé de répondre."); // la bascule passe au fournisseur suivant
        }

        return text;
    }

    private async Task<string> ChatOnceAsync(string spec, IEnumerable<object> messages, CancellationToken ct, int maxTokens, bool think)
    {
        var (url, model, _) = Resolve(spec);
        if (Gateway(url) is AiNode gateway)
        {
            if (gateway.Api == "anthropic")
            {
                return await ChatClaudeAsync(gateway, model, messages, ct, maxTokens, think);
            }

            // Corps minimal : model + messages + max_tokens (pas d'échantillonnage, certains modèles le refusent → 502).
            var gw = new { model, messages, max_tokens = Math.Max(maxTokens, think ? 2048 : 512) };
            using var gResp = await SendAsync(HttpMethod.Post, GatewayUrl(gateway, "/chat/completions"), JsonContent.Create(gw), ct);
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
            if (node.Api == "anthropic")
            {
                request.Headers.Add("x-api-key", node.Token);
                request.Headers.Add("anthropic-version", "2023-06-01");
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", node.Token);
            }
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
        throw new AiHttpException((int)resp.StatusCode, body);
    }
}

/// <summary>Réponse d'erreur d'une machine ou d'un fournisseur ; le code et le corps servent à la bascule (crédit épuisé).</summary>
public sealed class AiHttpException : InvalidOperationException
{
    public AiHttpException(int status, string body)
        : base($"Le fournisseur a répondu {status} : {body}")
    {
        Status = status;
        Body = body;
    }

    public int Status { get; }

    public string Body { get; }
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

    /// <summary>Minutes restantes avant de réessayer ce fournisseur (crédit épuisé) ; 0 = en service.</summary>
    public int PausedMinutes { get; set; }
}

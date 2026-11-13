namespace SecondMonitor.Hub.Services;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Diffuse l'état de course à tous les navigateurs connectés (Mac, iPad, téléphone…) via WebSocket, 5 fois par seconde.
/// Les messages sont des objets JSON { type, data }.
/// Un collecteur peut aussi s'y connecter comme « poste audio » (/ws?poste=1, puis { type: "station", voice, mic, name }) :
/// il joue la voix de l'ingénieur et/ou enregistre le micro sur le PC de jeu ; il ne reçoit pas l'état de course.
/// </summary>
public sealed class LiveHub : BackgroundService
{
    public static readonly JsonSerializerOptions Json = CreateJson();

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<WebSocket, SemaphoreSlim> SendLocks = new();

    private static JsonSerializerOptions CreateJson()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        FiniteNumbers.Register(o); // jamais de « "NaN" » vers l'interface (écran noir)
        return o;
    }

    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly ConcurrentDictionary<Guid, AudioStation> _stations = new();
    private readonly ConcurrentDictionary<Guid, bool> _noSnapshots = new();
    private readonly RaceEngine _engine;
    private readonly List<object> _radioHistory = new();
    private long _lastSequence = -1;
    private string _voiceOutput;

    public LiveHub(RaceEngine engine, SettingsStore settings)
    {
        _engine = engine;
        _voiceOutput = settings.Current.VoiceOutput;
        settings.Changed += s =>
        {
            // « Voix de l'ingénieur sur » changé dans Réglages : les écrans et le PC de jeu l'apprennent tout de suite.
            if (s.VoiceOutput != _voiceOutput)
            {
                _voiceOutput = s.VoiceOutput;
                _ = BroadcastStationsAsync();
            }
        };
    }

    public int ClientCount => _clients.Count;

    /// <summary>Postes audio déclarés (collecteur du PC de jeu : voix de l'ingénieur, micro).</summary>
    public AudioStation[] Stations => _stations.Values.OrderBy(s => s.Since).ToArray();

    /// <summary>Poste audio qui enregistre le micro : les appuis radio venus d'ailleurs (ClickDeck) lui reviennent.</summary>
    public AudioStation? MicStation => Stations.FirstOrDefault(s => s.Mic);

    public async Task AcceptAsync(WebSocket socket, CancellationToken token, bool station = false, string? address = null)
    {
        var id = Guid.NewGuid();
        _clients[id] = socket;
        if (station)
        {
            _noSnapshots[id] = true; // poste audio : ni état de course ni historique, seulement la radio
        }

        try
        {
            if (!station)
            {
                await SendAsync(socket, Serialize("snapshot", _engine.BuildSnapshot()), token);
                object[] history;
                lock (_radioHistory)
                {
                    history = _radioHistory.ToArray();
                }

                await SendAsync(socket, Serialize("radioHistory", history), token);
            }

            await SendAsync(socket, Serialize("stations", StationsMessage()), token);
            var buffer = new byte[4096];
            using var message = new MemoryStream();
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text || message.Length + result.Count > 64 * 1024)
                {
                    message.SetLength(0); // seuls de petits messages texte sont attendus
                    continue;
                }

                message.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    await OnClientMessageAsync(id, message.ToArray(), address);
                    message.SetLength(0);
                }
            }
        }
        catch (Exception) when (token.IsCancellationRequested || socket.State != WebSocketState.Open)
        {
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            _clients.TryRemove(id, out _);
            _noSnapshots.TryRemove(id, out _);
            if (_stations.TryRemove(id, out _))
            {
                _ = BroadcastStationsAsync();
            }

            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    /// <summary>Dernière réponse de l'ingénieur (pour les petits écrans : ClickDeck…).</summary>
    public string? LastRadioText { get; private set; }

    /// <summary>Envoie un message ponctuel (ex. radio de l'IA, demande de stand) à tous les clients.</summary>
    public Task BroadcastAsync(string type, object data)
    {
        if (type == "radio" && data is EngineerReply last)
        {
            LastRadioText = last.Answer;
        }

        if (type == "radio")
        {
            lock (_radioHistory)
            {
                // Historique sans l'audio (trop lourd à renvoyer à chaque nouvel écran).
                _radioHistory.Add(data is EngineerReply r ? new EngineerReply { Question = r.Question, Transcript = r.Transcript, Answer = r.Answer, From = r.From, Facts = r.Facts, Milliseconds = r.Milliseconds, At = r.At, Kind = r.Kind, Proactive = r.Proactive, AwaitingAnswer = r.AwaitingAnswer, ListenMilliseconds = r.ListenMilliseconds, ThinkMilliseconds = r.ThinkMilliseconds, UsedPreparedAnswer = r.UsedPreparedAnswer, Category = r.Category, CategorySource = r.CategorySource } : data);
                if (_radioHistory.Count > 50)
                {
                    _radioHistory.RemoveAt(0);
                }
            }
        }

        return BroadcastRawAsync(Serialize(type, data));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_clients.IsEmpty)
            {
                continue;
            }

            RaceSnapshot snapshot = _engine.BuildSnapshot();
            if (snapshot.Sequence == _lastSequence)
            {
                continue;
            }

            _lastSequence = snapshot.Sequence;
            await BroadcastRawAsync(Serialize("snapshot", snapshot), snapshot: true);
        }
    }

    /// <summary>Message d'un client : seule la déclaration d'un poste audio est comprise.</summary>
    private async Task OnClientMessageAsync(Guid id, byte[] payload, string? address)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out JsonElement type))
            {
                return;
            }

            if (type.GetString() == "pitMenuResult" && _stations.ContainsKey(id))
            {
                // Le collecteur a réglé (ou lu) le menu des stands de Le Mans Ultimate.
                string text = root.TryGetProperty("text", out JsonElement t) && t.ValueKind == JsonValueKind.String ? t.GetString()! : "Menu des stands : pas de réponse lisible.";
                bool ok = root.TryGetProperty("ok", out JsonElement okEl) && okEl.ValueKind == JsonValueKind.True;
                bool test = root.TryGetProperty("test", out JsonElement testEl) && testEl.ValueKind == JsonValueKind.True;
                string reqId = root.TryGetProperty("id", out JsonElement idEl) ? idEl.ToString() : string.Empty;
                PitMenuResult?.Invoke(new PitMenuReply(reqId, ok, test, text.Length > 400 ? text[..400] : text));
                return;
            }

            if (type.GetString() != "station")
            {
                return;
            }

            bool voice = root.TryGetProperty("voice", out JsonElement v) && v.ValueKind == JsonValueKind.True;
            bool mic = root.TryGetProperty("mic", out JsonElement m) && m.ValueKind == JsonValueKind.True;
            bool pit = root.TryGetProperty("pit", out JsonElement p) && p.ValueKind == JsonValueKind.True;
            string name = root.TryGetProperty("name", out JsonElement n) && n.ValueKind == JsonValueKind.String ? n.GetString()! : "PC de jeu";
            if (name.Length > 80)
            {
                name = name[..80];
            }

            if (!voice && !mic && !pit)
            {
                if (_stations.TryRemove(id, out _))
                {
                    await BroadcastStationsAsync();
                }

                return;
            }

            DateTime since = _stations.TryGetValue(id, out AudioStation? before) ? before.Since : DateTime.Now;
            var station = new AudioStation(name, voice, mic, address, since, pit);
            if (before != station)
            {
                _stations[id] = station;
                await BroadcastStationsAsync();
            }
        }
        catch (JsonException)
        {
            // Message illisible : ignoré.
        }
    }

    /// <summary>Résultat du menu des stands de Le Mans Ultimate renvoyé par le collecteur.</summary>
    public event Action<PitMenuReply>? PitMenuResult;

    /// <summary>Un collecteur peut régler le menu des stands de Le Mans Ultimate.</summary>
    public bool HasPitStation => _stations.Values.Any(s => s.Pit);

    /// <summary>Envoie une demande de menu des stands au collecteur qui sait le faire (« pitMenu » ou « pitMenuTest »). Faux si aucun.</summary>
    public async Task<bool> SendToPitStationAsync(string type, object data, CancellationToken ct)
    {
        foreach (var (id, station) in _stations.Where(x => x.Value.Pit).OrderBy(x => x.Value.Since))
        {
            if (_clients.TryGetValue(id, out WebSocket? socket) && socket.State == WebSocketState.Open)
            {
                await SendAsync(socket, Serialize(type, data), ct);
                return true;
            }
        }

        return false;
    }

    private object StationsMessage() => new { stations = Stations, voiceOutput = _voiceOutput };

    private Task BroadcastStationsAsync() => BroadcastRawAsync(Serialize("stations", StationsMessage()));

    private static byte[] Serialize(string type, object data) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type, data }, Json));

    private async Task BroadcastRawAsync(byte[] payload, bool snapshot = false)
    {
        foreach (var (id, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                _clients.TryRemove(id, out _);
                continue;
            }

            if (snapshot && _noSnapshots.ContainsKey(id))
            {
                continue;
            }

            try
            {
                await SendAsync(socket, payload, CancellationToken.None);
            }
            catch (Exception)
            {
                _clients.TryRemove(id, out _);
            }
        }
    }

    private static async Task SendAsync(WebSocket socket, byte[] payload, CancellationToken token)
    {
        // Un envoi à la fois par client : la radio (avec sa voix) peut partir pendant l'état de course,
        // et deux envois simultanés font échouer le second (le client ne recevait alors plus rien).
        SemaphoreSlim gate = SendLocks.GetValue(socket, _ => new SemaphoreSlim(1, 1));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        await gate.WaitAsync(cts.Token);
        try
        {
            await socket.SendAsync(payload, WebSocketMessageType.Text, true, cts.Token);
        }
        finally
        {
            gate.Release();
        }
    }
}

/// <summary>
/// Poste audio : un collecteur qui joue la voix de l'ingénieur (<paramref name="Voice"/>) et/ou enregistre le micro
/// (<paramref name="Mic"/>) sur le PC de jeu.
/// </summary>
public sealed record AudioStation(string Name, bool Voice, bool Mic, string? Address, DateTime Since, bool Pit = false);

/// <summary>Réponse du collecteur pour le menu des stands de Le Mans Ultimate.</summary>
public sealed record PitMenuReply(string Id, bool Ok, bool Test, string Text);

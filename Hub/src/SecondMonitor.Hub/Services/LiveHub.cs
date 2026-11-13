namespace SecondMonitor.Hub.Services;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Diffuse l'état de course à tous les navigateurs connectés (Mac, iPad, téléphone…) via WebSocket, 5 fois par seconde.
/// Les messages sont des objets JSON { type, data }.
/// </summary>
public sealed class LiveHub : BackgroundService
{
    public static readonly JsonSerializerOptions Json = CreateJson();

    private static JsonSerializerOptions CreateJson()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        FiniteNumbers.Register(o); // jamais de « "NaN" » vers l'interface (écran noir)
        return o;
    }

    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly RaceEngine _engine;
    private readonly List<object> _radioHistory = new();
    private long _lastSequence = -1;

    public LiveHub(RaceEngine engine)
    {
        _engine = engine;
    }

    public int ClientCount => _clients.Count;

    public async Task AcceptAsync(WebSocket socket, CancellationToken token)
    {
        var id = Guid.NewGuid();
        _clients[id] = socket;
        try
        {
            await SendAsync(socket, Serialize("snapshot", _engine.BuildSnapshot()), token);
            object[] history;
            lock (_radioHistory)
            {
                history = _radioHistory.ToArray();
            }

            await SendAsync(socket, Serialize("radioHistory", history), token);
            var buffer = new byte[4096];
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
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

    /// <summary>Envoie un message ponctuel (ex. radio de l'IA, demande de stand) à tous les clients.</summary>
    /// <summary>Dernière réponse de l'ingénieur (pour les petits écrans : ClickDeck…).</summary>
    public string? LastRadioText { get; private set; }

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
            await BroadcastRawAsync(Serialize("snapshot", snapshot));
        }
    }

    private static byte[] Serialize(string type, object data) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type, data }, Json));

    private async Task BroadcastRawAsync(byte[] payload)
    {
        foreach (var (id, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                _clients.TryRemove(id, out _);
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
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        await socket.SendAsync(payload, WebSocketMessageType.Text, true, cts.Token);
    }
}

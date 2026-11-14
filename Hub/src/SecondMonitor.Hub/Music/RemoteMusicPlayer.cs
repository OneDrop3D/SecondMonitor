namespace SecondMonitor.Hub.Music;

using System.Text.Json;
using SecondMonitor.Hub.Services;

/// <summary>
/// Deezer du PC de jeu, piloté par le collecteur : la recherche et les décisions restent sur cet ordinateur, la musique
/// joue dans l'application Deezer du PC de jeu (dans le casque du pilote). Les commandes partent par la liaison radio du
/// collecteur (« music ») et sa réponse donne l'état de Deezer.
/// </summary>
public sealed class RemoteMusicPlayer : IMusicPlayer
{
    private readonly LiveHub _hub;
    private readonly Func<MusicSettings> _settings;
    private bool? _installed;
    private bool _running;
    private bool _canSetVolume = true;

    public RemoteMusicPlayer(LiveHub hub, Func<MusicSettings> settings)
    {
        _hub = hub;
        _settings = settings;
    }

    /// <summary>Un collecteur a déclaré la musique sur le PC de jeu.</summary>
    public bool Connected => _hub.HasMusicStation;

    public string Platform => "pc";

    public bool CanSetVolume => _canSetVolume;

    // Tant que le collecteur n'a rien dit, on suppose l'application Deezer installée (liens deezer://, lecture automatique).
    public bool AppInstalled() => _installed ?? true;

    public bool AppRunning() => _running;

    public async Task<PlayerState> GetStateAsync(CancellationToken ct) => await RequestAsync("state", null, null, ct) ?? new PlayerState();

    public async Task<bool> SendAsync(TransportCommand command, PlayerState? known, CancellationToken ct)
    {
        string action = command switch
        {
            TransportCommand.Play => "play",
            TransportCommand.Pause => "pause",
            TransportCommand.Next => "next",
            _ => "previous",
        };
        return await RequestAsync(action, null, null, ct) != null;
    }

    public async Task<int?> SetVolumeAsync(int pct, CancellationToken ct)
    {
        PlayerState? state = await RequestAsync("volume", null, Math.Clamp(pct, 0, 100), ct);
        return state?.Volume;
    }

    public async Task OpenAsync(string link, CancellationToken ct) => await RequestAsync("open", link, null, ct);

    public PermissionInfo Permission(bool mediaKeys) => new(false, true, null, null);

    public bool RequestPermission() => true;

    public void OpenPermissionSettings()
    {
    }

    private async Task<PlayerState?> RequestAsync(string action, string? link, int? volume, CancellationToken ct)
    {
        JsonElement? reply = await _hub.RequestMusicAsync(action, link, volume, _settings().Player == "web", ct);
        if (reply is not JsonElement r || !r.TryGetProperty("ok", out JsonElement ok) || ok.ValueKind != JsonValueKind.True)
        {
            return null;
        }

        if (r.TryGetProperty("installed", out JsonElement i) && i.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            _installed = i.GetBoolean();
        }

        _running = r.TryGetProperty("running", out JsonElement run) && run.ValueKind == JsonValueKind.True;
        _canSetVolume = !r.TryGetProperty("canSetVolume", out JsonElement v) || v.ValueKind != JsonValueKind.False;
        var state = new PlayerState { Via = "PC de jeu" };
        if (r.TryGetProperty("state", out JsonElement s) && s.ValueKind == JsonValueKind.Object)
        {
            state.Playing = s.TryGetProperty("playing", out JsonElement p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False ? p.GetBoolean() : null;
            state.Title = Str(s, "title");
            state.Artist = Str(s, "artist");
            state.Album = Str(s, "album");
            state.Source = Str(s, "source") ?? "Deezer (PC de jeu)";
            state.Volume = s.TryGetProperty("volume", out JsonElement vol) && vol.TryGetInt32(out int n) ? n : null;
            state.Via = "PC de jeu · " + (Str(s, "via") ?? "collecteur");
        }

        return state;
    }

    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

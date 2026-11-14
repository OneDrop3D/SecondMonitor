namespace SecondMonitor.Hub.Music;

using SecondMonitor.Hub.Core.Music;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Services;

/// <summary>
/// Musique Deezer pilotée par SecondMonitor : la lecture se fait dans l'application Deezer de l'ordinateur (ou le lecteur
/// web connecté), donc avec le compte et la pleine qualité de l'utilisateur ; le hub ne fait que chercher (API publique),
/// ouvrir les liens et envoyer lecture / pause / suivant / précédent / volume.
/// La radio passe avant : la musique s'efface quand le pilote appuie sur la radio ou quand l'ingénieur parle.
/// </summary>
public sealed class MusicService
{
    private static readonly HttpClient Http = CreateHttp();

    private readonly RaceEngine _engine;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly DuckPlanner _planner = new();

    // État de la priorité radio (sous _gate).
    private bool _duckPaused;
    private int? _duckVolume;
    private bool _resumeOnRestore;
    private bool _userPausedDuringDuck;
    private string? _pendingOpen;

    private (DateTime At, PlayerState State) _cache = (DateTime.MinValue, new PlayerState());
    private string? _lastSessionType;
    private readonly IMusicPlayer _local;
    private readonly RemoteMusicPlayer _remote;

    public MusicService(string dataDir, RaceEngine engine, ILogger logger, LiveHub hub)
    {
        _engine = engine;
        _logger = logger;
        Settings = new MusicSettingsStore(dataDir);
        _local = CreatePlayer(dataDir, () => Settings.Current);
        _remote = new RemoteMusicPlayer(hub, () => Settings.Current);
        // La voix de l'ingénieur dans le casque du PC de jeu : la musique s'efface aussi.
        hub.StationVoice += playing => SetVoice(playing);
    }

    public MusicSettingsStore Settings { get; }

    /// <summary>Deezer de cet ordinateur, ou celui du PC de jeu (réglage « Musique sur »).</summary>
    public IMusicPlayer Player => OnGamingPc ? _remote : _local;

    /// <summary>La musique joue sur le PC de jeu (réglage), par le collecteur.</summary>
    public bool OnGamingPc => Settings.Current.Output == "pc";

    /// <summary>Ce qui a été lancé en dernier (« la playlist Rock Essentials »), pour « c'est quoi ce morceau ? ».</summary>
    public string? LastOpened { get; private set; }

    /// <summary>Dernière action ou erreur, affichée sur la carte Musique.</summary>
    public string? LastMessage { get; private set; }

    public bool Ducked => _planner.Ducked;

    private static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("SecondMonitor-Hub");
        return http;
    }

    private static IMusicPlayer CreatePlayer(string dataDir, Func<MusicSettings> settings)
    {
        if (Environment.GetEnvironmentVariable("SMHUB_MUSIQUE_SIMULEE") == "1")
        {
            return new SimulatedMusicPlayer();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacMusicPlayer(dataDir, settings);
        }

        if (OperatingSystem.IsWindows())
        {
            return new WindowsMusicPlayer(dataDir, () => settings().Player == "web");
        }

        return new LinuxMusicPlayer();
    }

    // ───── Boucle : priorité radio et réglage « pas de musique en course » ─────

    public async Task RunAsync(CancellationToken ct)
    {
        int tick = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(150, ct);
                DateTime now = DateTime.UtcNow;
                _planner.SetRadio(DeckApi.RadioPhase is "recording" or "thinking", now);
                DuckStep step = _planner.Tick(now);
                if (step != DuckStep.None && Settings.Current.Enabled)
                {
                    await WithGateAsync(() => step == DuckStep.Duck ? DuckAsync(ct) : RestoreAsync(ct), ct);
                }
                else if (step == DuckStep.Restore)
                {
                    ResetDuck();
                }

                if (++tick % 13 == 0)
                {
                    await EnforceSessionPolicyAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Musique : boucle");
            }
        }
    }

    /// <summary>La voix de l'ingénieur commence ou s'arrête sur le poste radio (signalé par l'interface).</summary>
    public void SetVoice(bool playing) => _planner.SetVoice(playing, DateTime.UtcNow);

    private async Task DuckAsync(CancellationToken ct)
    {
        MusicSettings s = Settings.Current;
        if (s.DuckMode == "aucun")
        {
            return;
        }

        PlayerState state = await StateAsync(ct, fresh: true);
        if (s.DuckMode == "volume" && Player.CanSetVolume && state.Playing != false && state.Volume is int v && v > 0)
        {
            _duckVolume = v;
            await Player.SetVolumeAsync(v * s.DuckVolumePct / 100, ct);
        }
        else if (state.Playing == true)
        {
            // On ne met en pause que si l'on SAIT que Deezer joue : sinon la reprise lancerait une musique arrêtée exprès.
            await Player.SendAsync(TransportCommand.Pause, state, ct);
            _duckPaused = true;
        }
    }

    private async Task RestoreAsync(CancellationToken ct)
    {
        try
        {
            if (_duckVolume is int v)
            {
                await Player.SetVolumeAsync(v, ct);
            }

            if (_pendingOpen != null)
            {
                await Player.OpenAsync(_pendingOpen, ct);
            }
            else if ((_duckPaused && !_userPausedDuringDuck) || _resumeOnRestore)
            {
                if (SessionAllowed(out _))
                {
                    await Player.SendAsync(TransportCommand.Play, new PlayerState { Playing = false }, ct);
                }
            }
        }
        finally
        {
            ResetDuck();
            _cache = (DateTime.MinValue, _cache.State);
        }
    }

    private void ResetDuck()
    {
        _duckPaused = false;
        _duckVolume = null;
        _resumeOnRestore = false;
        _userPausedDuringDuck = false;
        _pendingOpen = null;
    }

    private async Task EnforceSessionPolicyAsync(CancellationToken ct)
    {
        string? session = _engine.BuildSnapshot().SessionType;
        if (session == _lastSessionType)
        {
            return;
        }

        _lastSessionType = session;
        if (!Settings.Current.Enabled)
        {
            return;
        }

        if (SessionAllowed(out string? reason))
        {
            // Nouvelle session où la musique est permise : la playlist préférée démarre toute seule (si rien ne joue).
            if (session != null && Settings.Current.FavoriteAutoPlay && Settings.Current.Favorite is { } fav)
            {
                await WithGateAsync(async () =>
                {
                    PlayerState state = await StateAsync(ct, fresh: true);
                    if (state.Playing != true)
                    {
                        string label = MusicCommands.Describe(new DeezerItem { Kind = Kind(fav), Id = fav.Id, Title = fav.Title });
                        LastMessage = await OpenOrQueueAsync(FavoriteLink(fav), label, _planner.Ducked, ct, $"Début de session : {label}.");
                    }
                }, ct);
            }

            return;
        }

        // La session interdit la musique (départ de la course) : on la coupe une fois.
        await WithGateAsync(async () =>
        {
            PlayerState state = await StateAsync(ct, fresh: true);
            if (state.Playing == true)
            {
                await Player.SendAsync(TransportCommand.Pause, state, ct);
                LastMessage = reason;
            }

            _resumeOnRestore = false;
            _duckPaused = false;
            _pendingOpen = null;
        }, ct);
    }

    public bool SessionAllowed(out string? reason)
    {
        bool ok = MusicPolicy.Allowed(Settings.Current.SessionPolicy, _engine.BuildSnapshot().SessionType, out string r);
        reason = r;
        return ok;
    }

    // ───── Commandes (voix, carte Musique, API) ─────

    /// <summary>Phrase radio : commande musicale traitée sans le modèle IA. Null si ce n'est pas de la musique.</summary>
    public async Task<string?> HandleVoiceAsync(string text, bool test, CancellationToken ct)
    {
        MusicIntent? intent = MusicCommands.Parse(text);
        if (intent == null)
        {
            return null;
        }

        if (!Settings.Current.Enabled)
        {
            return intent.Explicit ? "La musique Deezer est désactivée. Active-la dans les réglages, carte Musique." : null;
        }

        if (test)
        {
            if (intent.Action == MusicAction.Open)
            {
                DeezerItem? found = await FindAsync(intent, ct);
                return found == null
                    ? (intent.Explicit ? $"Mode test : je n'ai pas trouvé {intent.Query} sur Deezer." : null)
                    : $"Mode test : je lancerais {MusicCommands.Describe(found)}. Rien n'est envoyé à Deezer.";
            }

            return $"Mode test : commande musique « {Label(intent.Action)} » reconnue. Rien n'est envoyé à Deezer.";
        }

        return await ExecuteAsync(intent, ct);
    }

    /// <summary>Exécute une commande et rend la phrase à dire (courte). Null : « mets X » qui n'était pas de la musique.</summary>
    public async Task<string?> ExecuteAsync(MusicIntent intent, CancellationToken ct)
    {
        if (!Settings.Current.Enabled)
        {
            return Remember("La musique Deezer est désactivée. Active-la dans les réglages, carte Musique.");
        }

        if ((intent.Action is MusicAction.Play or MusicAction.Flow or MusicAction.Open) && !SessionAllowed(out string? reason))
        {
            return Remember(reason!);
        }

        // La recherche se fait hors du verrou (réseau) ; tout le reste est séquentiel (pas deux commandes en même temps).
        DeezerItem? item = null;
        if (intent.Action == MusicAction.Open)
        {
            try
            {
                item = await FindAsync(intent, ct);
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) && !ct.IsCancellationRequested)
            {
                return Remember("Deezer ne répond pas, je ne peux pas chercher pour le moment.");
            }

            if (item == null)
            {
                return intent.Explicit ? Remember($"Je n'ai pas trouvé {intent.Query} sur Deezer.") : null;
            }
        }

        string? reply = null;
        LastMessage = null;
        await WithGateAsync(async () => reply = await ExecuteLockedAsync(intent, item, ct), ct);
        return Remember(reply ?? LastMessage ?? "La commande musique n'a pas pu être envoyée.");
    }

    private async Task<string> ExecuteLockedAsync(MusicIntent intent, DeezerItem? item, CancellationToken ct)
    {
        MusicSettings s = Settings.Current;
        bool ducked = _planner.Ducked;
        switch (intent.Action)
        {
            case MusicAction.Pause:
            {
                if (ducked)
                {
                    _userPausedDuringDuck = true;
                    _resumeOnRestore = false;
                    _pendingOpen = null;
                    if (_duckPaused)
                    {
                        return "Musique en pause.";
                    }
                }

                PlayerState state = await StateAsync(ct, fresh: true);
                await Player.SendAsync(TransportCommand.Pause, state, ct);
                return "Musique en pause.";
            }

            case MusicAction.Play:
            {
                if (NeedsLaunch(out string flowLink))
                {
                    if (s.Favorite is { } fav)
                    {
                        return await OpenFavoriteAsync(fav, ducked, ct);
                    }

                    return await OpenOrQueueAsync(flowLink, "Flow", ducked, ct, "Je lance Deezer avec ton Flow.");
                }

                if (ducked)
                {
                    _resumeOnRestore = true;
                    _userPausedDuringDuck = false;
                    return "C'est parti.";
                }

                PlayerState state = await StateAsync(ct, fresh: true);
                if (state.Playing == true)
                {
                    return "La musique tourne déjà.";
                }

                await Player.SendAsync(TransportCommand.Play, state, ct);
                await Task.Delay(1500, ct);
                PlayerState after = await StateAsync(ct, fresh: true);
                if (after.Playing == false && state.Title == null)
                {
                    // Rien à reprendre dans Deezer : la playlist préférée, sinon le Flow.
                    return s.Favorite is { } fav
                        ? await OpenFavoriteAsync(fav, false, ct)
                        : await OpenOrQueueAsync(FlowLink(), "ton Flow", false, ct, "Je lance ton Flow.");
                }

                if (after.Playing == false)
                {
                    return Player.Platform == "mac" && !s.MediaKeys
                        ? "Deezer ne répond pas à la commande. Essaie les touches multimédia dans la carte Musique."
                        : "Deezer ne répond pas à la commande.";
                }

                return "C'est parti.";
            }

            case MusicAction.Next:
            case MusicAction.Previous:
            {
                PlayerState state = await StateAsync(ct, fresh: true);
                await Player.SendAsync(intent.Action == MusicAction.Next ? TransportCommand.Next : TransportCommand.Previous, state, ct);
                if (ducked && _duckPaused && s.DuckMode == "pause")
                {
                    // Deezer repart souvent tout seul sur le nouveau morceau : on le remet en pause jusqu'à la fin de la radio.
                    await Task.Delay(500, ct);
                    await Player.SendAsync(TransportCommand.Pause, null, ct);
                    _resumeOnRestore = true;
                }

                return intent.Action == MusicAction.Next ? "Morceau suivant." : "Morceau précédent.";
            }

            case MusicAction.VolumeUp:
            case MusicAction.VolumeDown:
            {
                if (!Player.CanSetVolume)
                {
                    return "Sur Mac, je ne peux pas régler le volume de Deezer seul : utilise le curseur de Deezer. Le volume de l'ordinateur changerait aussi ma voix.";
                }

                PlayerState state = await StateAsync(ct, fresh: true);
                int current = _duckVolume ?? state.Volume ?? 50;
                int target = Math.Clamp(current + (intent.Action == MusicAction.VolumeUp ? s.VolumeStepPct : -s.VolumeStepPct), 5, 100);
                if (ducked && _duckVolume != null)
                {
                    _duckVolume = target; // appliqué à la fin de la radio
                }
                else if (await Player.SetVolumeAsync(target, ct) == null)
                {
                    return "Je ne trouve pas Deezer pour régler son volume.";
                }

                return $"Musique à {target} %.";
            }

            case MusicAction.NowPlaying:
            {
                PlayerState state = await StateAsync(ct, fresh: true);
                if (!string.IsNullOrWhiteSpace(state.Title))
                {
                    return string.IsNullOrWhiteSpace(state.Artist) ? $"C'est {state.Title}." : $"C'est {state.Title}, de {state.Artist}.";
                }

                return LastOpened != null
                    ? $"Je ne vois pas le titre exact sur cet ordinateur. Tu écoutes {LastOpened}."
                    : "Je ne vois pas le titre en cours sur cet ordinateur.";
            }

            case MusicAction.Flow:
                return await OpenOrQueueAsync(FlowLink(), "ton Flow", ducked, ct, "Je lance ton Flow.");

            case MusicAction.Favorite:
                return s.Favorite is { } favorite
                    ? await OpenFavoriteAsync(favorite, ducked, ct)
                    : "Pas encore de playlist préférée : choisis-la avec l'étoile dans la carte Musique.";

            case MusicAction.Open when item != null:
            {
                string label = MusicCommands.Describe(item);
                string link = UseApp() ? DeezerCatalog.AppLink(item.Kind, item.Id) : DeezerCatalog.WebLink(item.Kind, item.Id);
                return await OpenOrQueueAsync(link, label, ducked, ct, $"C'est parti : {label}.");
            }

            default:
                return "Je n'ai pas compris la commande musique.";
        }
    }

    /// <summary>Ouvre (ou ouvrira à la fin de la radio) un élément trouvé par la carte Musique.</summary>
    public async Task<string> OpenItemAsync(MusicKind kind, long id, string? title, CancellationToken ct)
    {
        if (!Settings.Current.Enabled)
        {
            return Remember("La musique Deezer est désactivée.");
        }

        if (!SessionAllowed(out string? reason))
        {
            return Remember(reason!);
        }

        string link = UseApp() ? DeezerCatalog.AppLink(kind, id) : DeezerCatalog.WebLink(kind, id);
        string label = MusicCommands.Describe(new DeezerItem { Kind = kind, Id = id, Title = title ?? "la sélection" });
        string reply = string.Empty;
        await WithGateAsync(async () => reply = await OpenOrQueueAsync(link, label, _planner.Ducked, ct, $"C'est parti : {label}."), ct);
        return Remember(reply);
    }

    public Task<List<DeezerItem>> SearchAsync(MusicKind kind, string query, CancellationToken ct) =>
        DeezerCatalog.SearchAsync(Http, kind, query, ct, 10);

    private Task<DeezerItem?> FindAsync(MusicIntent intent, CancellationToken ct) =>
        DeezerCatalog.FindAsync(Http, intent.Kind, intent.Query, intent.Explicit, ct)!;

    private async Task<string> OpenOrQueueAsync(string link, string label, bool ducked, CancellationToken ct, string reply)
    {
        LastOpened = label;
        if (ducked)
        {
            _pendingOpen = link; // ouvert à la fin de la radio (la musique ne démarre pas pendant que l'ingénieur parle)
            _userPausedDuringDuck = false;
            return reply;
        }

        if (OnGamingPc && !_remote.Connected)
        {
            return "La musique est réglée sur le PC de jeu, mais le collecteur ne la propose pas : ouvre le collecteur, case « Musique Deezer sur ce PC ».";
        }

        await Player.OpenAsync(link, ct);
        _cache = (DateTime.MinValue, _cache.State);
        bool web = !link.StartsWith("deezer:", StringComparison.Ordinal);
        WatchStart(link, web);
        return web && Settings.Current.Player == "app" ? reply + " Deezer n'est pas installé : je l'ouvre dans le navigateur." : reply;
    }

    /// <summary>
    /// Après l'ouverture : la lecture a-t-elle vraiment démarré ? Le lecteur web ignore souvent la lecture automatique
    /// (le navigateur la bloque) : on envoie « lecture » une fois, puis on le dit dans la carte Musique si rien ne joue.
    /// </summary>
    private void WatchStart(string link, bool web)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(7));
                if ((await Player.GetStateAsync(CancellationToken.None)).Playing == true || _planner.Ducked)
                {
                    return;
                }

                if (web)
                {
                    await WithGateAsync(() => Player.SendAsync(TransportCommand.Play, new PlayerState { Playing = false }, CancellationToken.None), CancellationToken.None);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    if ((await Player.GetStateAsync(CancellationToken.None)).Playing == true)
                    {
                        return;
                    }
                }

                LastMessage = web
                    ? "Deezer s'est ouvert dans le navigateur mais ne joue pas : le navigateur bloque la lecture automatique. Cliquez sur lecture dans Deezer, ou installez l'application Deezer (elle démarre toute seule)."
                    : "Deezer s'est ouvert mais ne joue pas encore : vérifiez que l'application Deezer est connectée à votre compte.";
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Musique : vérification du démarrage");
            }
        });
    }

    private Task<string> OpenFavoriteAsync(MusicFavorite fav, bool ducked, CancellationToken ct)
    {
        string label = MusicCommands.Describe(new DeezerItem { Kind = Kind(fav), Id = fav.Id, Title = fav.Title });
        return OpenOrQueueAsync(FavoriteLink(fav), label, ducked, ct, $"C'est parti : {label}.");
    }

    private string FavoriteLink(MusicFavorite fav) => UseApp() ? DeezerCatalog.AppLink(Kind(fav), fav.Id) : DeezerCatalog.WebLink(Kind(fav), fav.Id);

    private static MusicKind Kind(MusicFavorite fav) => DeezerCatalog.TryParseKind(fav.Kind, out MusicKind k) && k != MusicKind.Any ? k : MusicKind.Playlist;

    private bool UseApp() => Settings.Current.Player == "app" && Player.AppInstalled();

    private string FlowLink() => UseApp() ? DeezerCatalog.FlowAppLink : DeezerCatalog.FlowWebLink;

    /// <summary>L'application Deezer est installée mais fermée : il faut la lancer (avec le Flow).</summary>
    private bool NeedsLaunch(out string flowLink)
    {
        flowLink = FlowLink();
        return Settings.Current.Player == "app" ? !Player.AppRunning() : false;
    }

    // ───── État ─────

    public async Task<PlayerState> StateAsync(CancellationToken ct, bool fresh = false)
    {
        if (!fresh && DateTime.UtcNow - _cache.At < TimeSpan.FromSeconds(1.5))
        {
            return _cache.State;
        }

        PlayerState state = await Player.GetStateAsync(ct);
        _cache = (DateTime.UtcNow, state);
        return state;
    }

    public async Task<object> StatusAsync(CancellationToken ct)
    {
        MusicSettings s = Settings.Current;
        bool installed = Player.AppInstalled();
        bool running = Player.AppRunning();
        PlayerState? state = s.Enabled ? await StateAsync(ct) : null;
        bool allowed = SessionAllowed(out string? reason);
        PermissionInfo permission = Player.Permission(s.MediaKeys);
        return new
        {
            enabled = s.Enabled,
            settings = s,
            platform = Player.Platform,
            appInstalled = installed,
            appRunning = running,
            state,
            canSetVolume = Player.CanSetVolume,
            permission,
            ducked = _planner.Ducked,
            sessionType = _engine.BuildSnapshot().SessionType,
            sessionAllowed = allowed,
            sessionReason = reason,
            lastOpened = LastOpened,
            lastMessage = LastMessage,
            output = s.Output,
            pcConnected = _remote.Connected,
        };
    }

    /// <summary>Commande de la carte Musique (boutons) : même chemin que la voix.</summary>
    public Task<string?> CommandAsync(MusicAction action, CancellationToken ct) => ExecuteAsync(new MusicIntent { Action = action, Explicit = true }, ct);

    private async Task WithGateAsync(Func<Task> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Musique : commande impossible");
            LastMessage = $"Commande impossible : {ex.Message}";
        }
        finally
        {
            _cache = (DateTime.MinValue, _cache.State); // l'état a pu changer : relu au prochain affichage
            _gate.Release();
        }
    }

    private string Remember(string message)
    {
        LastMessage = message;
        return message;
    }

    private static string Label(MusicAction action) => action switch
    {
        MusicAction.Play => "lecture",
        MusicAction.Pause => "pause",
        MusicAction.Next => "morceau suivant",
        MusicAction.Previous => "morceau précédent",
        MusicAction.VolumeUp => "plus fort",
        MusicAction.VolumeDown => "moins fort",
        MusicAction.NowPlaying => "titre en cours",
        MusicAction.Flow => "Flow",
        MusicAction.Favorite => "playlist préférée",
        _ => "ouvrir",
    };
}

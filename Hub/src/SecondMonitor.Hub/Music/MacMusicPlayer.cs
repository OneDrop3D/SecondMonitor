namespace SecondMonitor.Hub.Music;

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;

/// <summary>
/// Deezer sur Mac. L'application Deezer n'a pas de dictionnaire AppleScript : on passe par le « À l'écoute » du système.
/// - Commandes (lecture, pause, suivant, précédent) : MediaRemote, appelé par osascript (programme d'Apple, sans autorisation).
///   Si ça ne suit pas (certaines versions de macOS le bloquent) : touches multimédia simulées, qui demandent l'autorisation
///   Accessibilité (option, demandée une seule fois).
/// - Titre en cours : MediaRemote (même chemin). Lecture en cours : CoreAudio (Deezer envoie-t-il du son ?).
/// - Volume de Deezer seul : impossible sur Mac (pas d'API) ; le volume du système baisserait aussi la voix de l'ingénieur.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacMusicPlayer : IMusicPlayer
{
    private const string InfoScript = """
        function run() {
          var out = {};
          try {
            $.NSBundle.bundleWithPath('/System/Library/PrivateFrameworks/MediaRemote.framework/').load;
            var R = $.NSClassFromString('MRNowPlayingRequest');
            try { var c = R.localNowPlayingPlayerPath.client; out.app = ObjC.unwrap(c.displayName); out.bundle = ObjC.unwrap(c.bundleIdentifier); } catch (e) {}
            try {
              var info = R.localNowPlayingItem.nowPlayingInfo;
              out.title = ObjC.unwrap(info.valueForKey('kMRMediaRemoteNowPlayingInfoTitle'));
              out.artist = ObjC.unwrap(info.valueForKey('kMRMediaRemoteNowPlayingInfoArtist'));
              out.album = ObjC.unwrap(info.valueForKey('kMRMediaRemoteNowPlayingInfoAlbum'));
              out.rate = ObjC.unwrap(info.valueForKey('kMRMediaRemoteNowPlayingInfoPlaybackRate'));
            } catch (e) {}
          } catch (e) { out.error = String(e); }
          return JSON.stringify(out);
        }
        """;

    private const string CommandScript = """
        function run(argv) {
          $.NSBundle.bundleWithPath('/System/Library/PrivateFrameworks/MediaRemote.framework/').load;
          ObjC.bindFunction('MRMediaRemoteSendCommand', ['bool', ['int', 'id']]);
          return String($.MRMediaRemoteSendCommand(parseInt(argv[0], 10), $()));
        }
        """;

    // Touche multimédia (NX_KEYTYPE_PLAY = 16, NEXT = 17, PREVIOUS = 18) : appui puis relâchement.
    private const string MediaKeyScript = """
        ObjC.import('AppKit');
        ObjC.import('CoreGraphics');
        function run(argv) {
          var key = parseInt(argv[0], 10);
          [0xa, 0xb].forEach(function (state) {
            var ev = $.NSEvent.otherEventWithTypeLocationModifierFlagsTimestampWindowNumberContextSubtypeData1Data2(
              14, { x: 0, y: 0 }, state << 8, 0, 0, $(), 8, (key << 16) | (state << 8), -1);
            $.CGEventPost(0, ev.CGEvent);
          });
          return 'ok';
        }
        """;

    private readonly string _scriptDir;
    private readonly Func<MusicSettings> _settings;

    public MacMusicPlayer(string dataDir, Func<MusicSettings> settings)
    {
        _settings = settings;
        _scriptDir = Path.Combine(dataDir, "musique");
        Directory.CreateDirectory(_scriptDir);
        File.WriteAllText(Path.Combine(_scriptDir, "info.js"), InfoScript);
        File.WriteAllText(Path.Combine(_scriptDir, "commande.js"), CommandScript);
        File.WriteAllText(Path.Combine(_scriptDir, "touche.js"), MediaKeyScript);
    }

    public string Platform => "mac";

    public bool CanSetVolume => false;

    private bool WebMode => _settings().Player == "web";

    public bool AppInstalled() =>
        Directory.Exists("/Applications/Deezer.app")
        || Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "Deezer.app"));

    public bool AppRunning()
    {
        Process[] list = Process.GetProcessesByName("Deezer");
        foreach (Process p in list)
        {
            p.Dispose();
        }

        return list.Length > 0;
    }

    public async Task<PlayerState> GetStateAsync(CancellationToken ct)
    {
        var state = new PlayerState();
        var (code, output) = await ProcessRunner.RunAsync("/usr/bin/osascript", new[] { "-l", "JavaScript", Path.Combine(_scriptDir, "info.js") }, ct, 3000);
        bool fromDeezer = false;
        if (code == 0 && output.StartsWith('{'))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(output);
                JsonElement r = doc.RootElement;
                string? app = Str(r, "app");
                string? bundle = Str(r, "bundle");
                fromDeezer = (bundle ?? app ?? string.Empty).Contains("deezer", StringComparison.OrdinalIgnoreCase);
                if (fromDeezer || (WebMode && (app != null || bundle != null)))
                {
                    state.Source = app ?? bundle;
                    state.Title = Str(r, "title");
                    state.Artist = Str(r, "artist");
                    state.Album = Str(r, "album");
                    if (r.TryGetProperty("rate", out JsonElement rate) && rate.ValueKind == JsonValueKind.Number)
                    {
                        state.Playing = rate.GetDouble() > 0;
                        state.Via = "MediaRemote";
                    }
                }
            }
            catch (JsonException)
            {
                // sortie inattendue : on garde ce qu'on sait
            }
        }

        if (!WebMode)
        {
            // L'application Deezer envoie-t-elle du son ? (fiable même si « À l'écoute » est bloqué par macOS)
            bool? audio = MacNative.IsAppPlayingAudio("deezer");
            if (audio != null && (state.Playing == null || !fromDeezer))
            {
                state.Playing = audio;
                state.Via = "CoreAudio";
                state.Source ??= "Deezer";
            }
        }

        return state;
    }

    public async Task<bool> SendAsync(TransportCommand command, PlayerState? known, CancellationToken ct)
    {
        bool keys = _settings().MediaKeys && MacNative.IsTrusted();
        if (keys)
        {
            return await SendKeyAsync(command, known, ct);
        }

        int code = command switch
        {
            TransportCommand.Play => 0,
            TransportCommand.Pause => 1,
            TransportCommand.Next => 4,
            _ => 5,
        };
        var (exit, _) = await ProcessRunner.RunAsync("/usr/bin/osascript", new[] { "-l", "JavaScript", Path.Combine(_scriptDir, "commande.js"), code.ToString() }, ct, 3000);

        // Lecture / pause sans effet (MediaRemote bloqué par cette version de macOS) : touches multimédia si permises.
        if (known?.Playing != null && (command is TransportCommand.Play or TransportCommand.Pause) && MacNative.IsTrusted())
        {
            await Task.Delay(900, ct);
            PlayerState after = await GetStateAsync(ct);
            bool wanted = command == TransportCommand.Play;
            if (after.Playing != null && after.Playing != wanted)
            {
                return await SendKeyAsync(command, after, ct);
            }
        }

        return exit == 0;
    }

    private async Task<bool> SendKeyAsync(TransportCommand command, PlayerState? known, CancellationToken ct)
    {
        // La touche lecture est une bascule : on ne l'envoie que si l'état ne correspond pas déjà.
        if ((command == TransportCommand.Play && known?.Playing == true) || (command == TransportCommand.Pause && known?.Playing == false))
        {
            return true;
        }

        int key = command switch
        {
            TransportCommand.Next => 17,
            TransportCommand.Previous => 18,
            _ => 16,
        };
        var (exit, _) = await ProcessRunner.RunAsync("/usr/bin/osascript", new[] { "-l", "JavaScript", Path.Combine(_scriptDir, "touche.js"), key.ToString() }, ct, 3000);
        return exit == 0;
    }

    public Task<int?> SetVolumeAsync(int pct, CancellationToken ct) => Task.FromResult<int?>(null);

    public Task OpenAsync(string link, CancellationToken ct)
    {
        ProcessRunner.OpenWithSystem(link);
        return Task.CompletedTask;
    }

    public PermissionInfo Permission(bool mediaKeys) => new(
        mediaKeys,
        MacNative.IsTrusted(),
        "Accessibilité",
        "Pour simuler les touches lecture / suivant / précédent du clavier quand macOS bloque la commande directe de Deezer.");

    public bool RequestPermission() => MacNative.RequestTrust();

    public void OpenPermissionSettings() =>
        ProcessRunner.OpenWithSystem("x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility");

    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

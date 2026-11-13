namespace SecondMonitor.Hub.Music;

using System.Diagnostics;

/// <summary>
/// Deezer sur Linux : pas d'application officielle. Le lecteur web (Chrome, Firefox…) ou une application non officielle
/// (deezer-linux) se pilotent par MPRIS, le standard des lecteurs Linux, via le petit programme « playerctl »
/// (à installer : sudo apt install playerctl). Lecture, pause, suivant, précédent, titre et volume de Deezer seul.
/// </summary>
public sealed class LinuxMusicPlayer : IMusicPlayer
{
    private static readonly string[] Browsers = { "chromium", "chrome", "firefox", "brave", "vivaldi", "edge", "opera" };

    public string Platform => "linux";

    public bool CanSetVolume => true;

    public bool AppInstalled()
    {
        try
        {
            using Process? p = Process.Start(new ProcessStartInfo("xdg-mime", "query default x-scheme-handler/deezer") { RedirectStandardOutput = true, UseShellExecute = false });
            if (p == null)
            {
                return false;
            }

            string handler = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(2000);
            return handler.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool AppRunning()
    {
        Process[] list = Process.GetProcesses();
        bool running = list.Any(p =>
        {
            try
            {
                return p.ProcessName.Contains("deezer", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        });
        foreach (Process p in list)
        {
            p.Dispose();
        }

        return running;
    }

    /// <summary>Lecteur MPRIS à piloter : celui de Deezer s'il existe, sinon un navigateur (lecteur web).</summary>
    private static async Task<string?> PickPlayerAsync(CancellationToken ct)
    {
        var (code, output) = await ProcessRunner.RunAsync("playerctl", new[] { "-l" }, ct, 2000);
        if (code != 0)
        {
            return null;
        }

        string[] players = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? deezer = players.FirstOrDefault(p => p.Contains("deezer", StringComparison.OrdinalIgnoreCase));
        if (deezer != null)
        {
            return deezer;
        }

        foreach (string p in players.Where(p => Browsers.Any(b => p.Contains(b, StringComparison.OrdinalIgnoreCase))))
        {
            var (_, title) = await ProcessRunner.RunAsync("playerctl", new[] { "-p", p, "metadata", "--format", "{{xesam:url}} {{mpris:trackid}} {{xesam:title}}" }, ct, 2000);
            if (title.Contains("deezer", StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return players.FirstOrDefault(p => Browsers.Any(b => p.Contains(b, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<PlayerState> GetStateAsync(CancellationToken ct)
    {
        string? player = await PickPlayerAsync(ct);
        if (player == null)
        {
            return new PlayerState();
        }

        var (code, output) = await ProcessRunner.RunAsync("playerctl", new[] { "-p", player, "metadata", "--format", "{{status}}\t{{title}}\t{{artist}}\t{{album}}\t{{volume}}" }, ct, 2000);
        string[] f = code == 0 ? output.Split('\t') : Array.Empty<string>();
        var state = new PlayerState { Source = player.Split('.')[0], Via = "MPRIS" };
        if (f.Length >= 5)
        {
            state.Playing = f[0] == "Playing";
            state.Title = NullIfEmpty(f[1]);
            state.Artist = NullIfEmpty(f[2]);
            state.Album = NullIfEmpty(f[3]);
            if (double.TryParse(f[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
            {
                state.Volume = (int)Math.Round(v * 100);
            }
        }

        return state;
    }

    public async Task<bool> SendAsync(TransportCommand command, PlayerState? known, CancellationToken ct)
    {
        string? player = await PickPlayerAsync(ct);
        string verb = command switch
        {
            TransportCommand.Play => "play",
            TransportCommand.Pause => "pause",
            TransportCommand.Next => "next",
            _ => "previous",
        };
        var args = player == null ? new[] { verb } : new[] { "-p", player, verb };
        var (code, _) = await ProcessRunner.RunAsync("playerctl", args, ct, 2000);
        return code == 0;
    }

    public async Task<int?> SetVolumeAsync(int pct, CancellationToken ct)
    {
        string? player = await PickPlayerAsync(ct);
        if (player == null)
        {
            return null;
        }

        string level = (Math.Clamp(pct, 0, 100) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var (code, _) = await ProcessRunner.RunAsync("playerctl", new[] { "-p", player, "volume", level }, ct, 2000);
        return code == 0 ? Math.Clamp(pct, 0, 100) : null;
    }

    public Task OpenAsync(string link, CancellationToken ct)
    {
        ProcessRunner.OpenWithSystem(link);
        return Task.CompletedTask;
    }

    public PermissionInfo Permission(bool mediaKeys) => new(false, true, null, null);

    public bool RequestPermission() => true;

    public void OpenPermissionSettings()
    {
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

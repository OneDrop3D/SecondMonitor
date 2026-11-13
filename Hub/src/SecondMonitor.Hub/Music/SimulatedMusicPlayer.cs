namespace SecondMonitor.Hub.Music;

/// <summary>
/// Lecteur simulé (variable d'environnement SMHUB_MUSIQUE_SIMULEE=1) : essais et captures d'écran sans toucher
/// à la musique de l'ordinateur. Garde en mémoire les commandes reçues.
/// </summary>
public sealed class SimulatedMusicPlayer : IMusicPlayer
{
    private static readonly string[] Titles = { "Harder, Better, Faster, Stronger", "One More Time", "Around the World", "Digital Love" };
    private readonly object _lock = new();
    private int _index;
    private bool _playing;
    private int _volume = 60;

    public List<string> Log { get; } = new();

    public string Platform => OperatingSystem.IsMacOS() ? "mac" : OperatingSystem.IsWindows() ? "windows" : "linux";

    public bool CanSetVolume => !OperatingSystem.IsMacOS();

    public bool AppInstalled() => true;

    public bool AppRunning() => true;

    public Task<PlayerState> GetStateAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            return Task.FromResult(new PlayerState
            {
                Playing = _playing,
                Title = Titles[_index % Titles.Length],
                Artist = "Daft Punk",
                Album = "Discovery",
                Source = "Deezer (simulé)",
                Volume = CanSetVolume ? _volume : null,
                Via = "simulation",
            });
        }
    }

    public Task<bool> SendAsync(TransportCommand command, PlayerState? known, CancellationToken ct)
    {
        lock (_lock)
        {
            Log.Add(command.ToString());
            switch (command)
            {
                case TransportCommand.Play: _playing = true; break;
                case TransportCommand.Pause: _playing = false; break;
                case TransportCommand.Next: _index++; _playing = true; break;
                default: _index = Math.Max(0, _index - 1); _playing = true; break;
            }
        }

        return Task.FromResult(true);
    }

    public Task<int?> SetVolumeAsync(int pct, CancellationToken ct)
    {
        lock (_lock)
        {
            Log.Add($"Volume {pct}");
            _volume = Math.Clamp(pct, 0, 100);
            return Task.FromResult<int?>(CanSetVolume ? _volume : null);
        }
    }

    public Task OpenAsync(string link, CancellationToken ct)
    {
        lock (_lock)
        {
            Log.Add($"Ouvre {link}");
            _playing = true;
        }

        return Task.CompletedTask;
    }

    public PermissionInfo Permission(bool mediaKeys) => new(mediaKeys && OperatingSystem.IsMacOS(), false, "Accessibilité", "Simulation");

    public bool RequestPermission() => false;

    public void OpenPermissionSettings()
    {
    }
}

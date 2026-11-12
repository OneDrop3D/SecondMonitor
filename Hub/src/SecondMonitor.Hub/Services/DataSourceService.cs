namespace SecondMonitor.Hub.Services;

using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Network;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Alimente le moteur de course : soit depuis le PC de jeu (réseau), soit depuis la course de démonstration.
/// </summary>
public sealed class DataSourceService : BackgroundService
{
    private readonly SettingsStore _settings;
    private readonly RaceEngine _engine;
    private readonly ILogger<DataSourceService> _logger;
    private readonly List<string> _log = new();
    private RemoteReceiver? _receiver;
    private CancellationTokenSource? _demoCts;
    private Task? _demoTask;
    private CaptureWriter? _capture;
    private string? _lastLogText;
    private int _repeat;

    /// <summary>Enregistrement à rejouer au lieu d'écouter le réseau (option « --rejouer fichier »).</summary>
    public static string? ReplayFile { get; set; }

    public static double ReplaySpeed { get; set; } = 1;

    public DataSourceService(SettingsStore settings, RaceEngine engine, ILogger<DataSourceService> logger)
    {
        _settings = settings;
        _engine = engine;
        _logger = logger;
        _settings.Changed += OnSettingsChanged;
    }

    public object GetStatus()
    {
        ReceiverStatus? status = _receiver?.GetStatus();
        lock (_log)
        {
            return new
            {
                mode = _settings.Current.DemoMode ? "demo" : "game",
                demoSpeed = _settings.Current.DemoSpeed,
                receiver = status,
                lastUpdateSecondsAgo = _engine.LastUpdateUtc == DateTime.MinValue ? (double?)null : (DateTime.UtcNow - _engine.LastUpdateUtc).TotalSeconds,
                log = _log.TakeLast(30).ToList(),
                capture = _capture?.CurrentFile,
                replay = ReplayFile,
            };
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ReplayFile != null)
        {
            return Task.Run(() => ReplayAsync(ReplayFile, stoppingToken), stoppingToken);
        }

        StartReceiver();
        if (_settings.Current.DemoMode)
        {
            StartDemo();
        }

        stoppingToken.Register(() =>
        {
            StopDemo();
            _receiver?.Dispose();
        });
        return Task.CompletedTask;
    }

    private void StartReceiver()
    {
        HubSettings s = _settings.Current;
        _receiver = new RemoteReceiver(s.GamePort, s.GameHosts);
        _receiver.LogMessage += AddLog;
        // Enregistrement de ce que le jeu envoie : permet de rejouer une session pour comprendre un problème.
        _capture = new CaptureWriter(Path.Combine(_settings.DataDirectory, "enregistrements"));
        _receiver.Capture = _capture;
        _receiver.DataReceived += (data, kind) =>
        {
            if (!_settings.Current.DemoMode)
            {
                _engine.Process(data, kind);
            }
        };
        _receiver.Start();
    }

    /// <summary>Rejoue un enregistrement comme si le collecteur l'envoyait (même rythme, ou accéléré).</summary>
    private async Task ReplayAsync(string path, CancellationToken token)
    {
        AddLog($"Relecture de {Path.GetFileName(path)} (×{ReplaySpeed})");
        var decoder = new PayloadDecoder();
        DateTime? previous = null;
        int n = 0, errors = 0;
        foreach (var (time, data) in CaptureReader.Read(path))
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (previous is DateTime p)
            {
                double wait = (time - p).TotalMilliseconds / Math.Max(0.1, ReplaySpeed);
                if (wait > 1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(wait, 5000)), token);
                }
            }

            previous = time;
            try
            {
                var (dataSet, kind) = decoder.Decode(data);
                _engine.Process(dataSet, kind);
                n++;
            }
            catch (Exception ex)
            {
                if (errors++ < 20)
                {
                    AddLog($"Paquet {n} non traité : {ex.GetType().Name} : {ex.Message} {ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}");
                }
            }
        }

        AddLog($"Relecture terminée : {n} paquets, {errors} erreurs");
    }

    private void OnSettingsChanged(HubSettings s)
    {
        _receiver?.SetManualHosts(s.GameHosts);
        if (s.DemoMode && _demoTask == null)
        {
            _engine.Reset();
            StartDemo();
        }
        else if (!s.DemoMode && _demoTask != null)
        {
            StopDemo();
            _engine.Reset();
        }
    }

    private void StartDemo()
    {
        AddLog("Mode démonstration : course fictive en cours");
        _demoCts = new CancellationTokenSource();
        CancellationToken token = _demoCts.Token;
        _demoTask = Task.Run(async () =>
        {
            var sim = new DemoRaceSimulator(seed: Environment.TickCount, raceDurationSeconds: 3600);
            const double tick = 0.1;
            while (!token.IsCancellationRequested)
            {
                double speed = Math.Clamp(_settings.Current.DemoSpeed, 0.25, 20);
                _engine.Process(sim.Step(tick * speed));
                if (sim.SessionTime > 3600 + 600)
                {
                    sim = new DemoRaceSimulator(seed: Environment.TickCount, raceDurationSeconds: 3600);
                    _engine.Reset();
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(tick), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });
    }

    private void StopDemo()
    {
        _demoCts?.Cancel();
        try
        {
            _demoTask?.Wait(1000);
        }
        catch (AggregateException)
        {
        }

        _demoTask = null;
        _demoCts = null;
    }

    private void AddLog(string message)
    {
        _logger.LogInformation("{Message}", message);
        lock (_log)
        {
            // Un même message répété (reconnexions toutes les 6 s) ne doit pas effacer le reste du journal.
            if (message == _lastLogText && _log.Count > 0)
            {
                _repeat++;
                _log[^1] = $"{DateTime.Now:HH:mm:ss} {message} (×{_repeat + 1})";
                return;
            }

            _lastLogText = message;
            _repeat = 0;
            string line = $"{DateTime.Now:HH:mm:ss} {message}";
            _log.Add(line);
            if (_log.Count > 200)
            {
                _log.RemoveRange(0, 100);
            }

            HubJournal.Write(_settings.DataDirectory, "réception", message);
        }
    }

    public override void Dispose()
    {
        _capture?.Dispose();
        base.Dispose();
    }
}

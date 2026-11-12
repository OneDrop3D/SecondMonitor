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
            };
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        _receiver.DataReceived += (data, kind) =>
        {
            if (!_settings.Current.DemoMode)
            {
                _engine.Process(data, kind);
            }
        };
        _receiver.Start();
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
            _log.Add($"{DateTime.Now:HH:mm:ss} {message}");
            if (_log.Count > 200)
            {
                _log.RemoveRange(0, 100);
            }
        }
    }
}

namespace SecondMonitor.Hub.Services;

using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Fait parler l'ingénieur IA de lui-même (pluie, relais à allonger, box imminent, menace, surchauffe), toutes les 3 s,
/// si l'option « L'IA prend la parole d'elle-même » est active. Les questions posées attendent la réponse du pilote,
/// qui est gardée dans la mémoire de course.
/// </summary>
public sealed class ProactiveService : BackgroundService
{
    private readonly RaceEngine _engine;
    private readonly EngineerService _engineer;
    private readonly SettingsStore _settings;
    private readonly ILogger<ProactiveService> _logger;
    private readonly ProactiveEngineer _rules = new();

    public ProactiveService(RaceEngine engine, EngineerService engineer, SettingsStore settings, ILogger<ProactiveService> logger)
    {
        _engine = engine;
        _engineer = engineer;
        _settings = settings;
        _logger = logger;
        engine.SessionReset += () =>
        {
            lock (_rules)
            {
                _rules.Reset();
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            AiSettings ai = _settings.Current.Ai;
            if (!ai.ProactiveRadio || (DateTime.UtcNow - _engine.LastUpdateUtc).TotalSeconds > 5)
            {
                continue;
            }

            try
            {
                RaceSnapshot snap = _engine.BuildSnapshot();
                ProactiveMessage? message;
                lock (_rules)
                {
                    message = _rules.Evaluate(snap, _engine.BuildWeather(), _engineer.Memory, new ProactiveOptions
                    {
                        WetThresholdPct = ai.WetThresholdPct,
                        MinIntervalSeconds = Math.Max(30, ai.ProactiveMinIntervalSec),
                    }, DateTime.UtcNow);
                }

                if (message == null)
                {
                    continue;
                }

                if (message.IsQuestion)
                {
                    _engineer.Memory.Ask(new PendingQuestion
                    {
                        Key = message.Key,
                        Topic = message.Topic,
                        Text = message.Text,
                        Lap = snap.Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps ?? 0,
                        PlannedLap = message.PlannedLap,
                        ProposedLap = message.ProposedLap,
                    });
                }

                await _engineer.SayAsync(message.Text, "ingénieur IA", stoppingToken, proactive: true, awaitingAnswer: message.IsQuestion);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Intervention spontanée de l'IA impossible");
            }
        }
    }
}

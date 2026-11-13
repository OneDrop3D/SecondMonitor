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
    private readonly RaceFinishRadio _finish = new();

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
                _finish.Reset();
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Toutes les 250 ms : alertes de pilotage (limites de piste, temps annulé, pénalité, usure brutale), dites tout de suite.
        // Toutes les 3 s : le reste (pluie, relais, arrêt, duels, surchauffe), avec un intervalle minimal entre deux messages.
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        DateTime nextRules = DateTime.MinValue;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            AiSettings ai = _settings.Current.Ai;
            List<DrivingEvent> driving = _engine.DrainDrivingEvents();
            if ((DateTime.UtcNow - _engine.LastUpdateUtc).TotalSeconds > 5)
            {
                continue;
            }

            if (!ai.TrackLimitAlerts)
            {
                driving.RemoveAll(e => e.Type is "limites" or "tour-annulé"); // coupées à la voix ou dans les réglages
            }

            if (driving.Count > 0 && ai.DrivingAlerts)
            {
                await SayDrivingAsync(driving, stoppingToken);
            }

            if (!ai.ProactiveRadio || DateTime.UtcNow < nextRules)
            {
                continue;
            }

            nextRules = DateTime.UtcNow.AddSeconds(3);

            try
            {
                RaceSnapshot snap = _engine.BuildSnapshot();

                // Arrivée de la course : la radio du résultat (une fois).
                string? finish;
                lock (_rules)
                {
                    finish = _finish.Check(snap);
                }

                if (finish != null)
                {
                    await _engineer.SayAsync(finish, "ingénieur IA", stoppingToken, proactive: true, category: ExchangeCategorizer.RaceInfo);
                    continue;
                }

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

                await _engineer.SayAsync(message.Text, "ingénieur IA", stoppingToken, proactive: true, awaitingAnswer: message.IsQuestion, category: ExchangeCategorizer.FromProactiveTopic(message.Topic));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Intervention spontanée de l'IA impossible");
            }
        }
    }

    /// <summary>Alertes de pilotage : une phrase courte, sans attendre l'intervalle des autres messages.</summary>
    private async Task SayDrivingAsync(List<DrivingEvent> events, CancellationToken ct)
    {
        try
        {
            RaceSnapshot snap = _engine.BuildSnapshot();
            foreach (RadioLine line in DrivingRadio.Lines(events, snap))
            {
                if (line.IsQuestion)
                {
                    _engineer.Memory.Ask(new PendingQuestion
                    {
                        Key = "usure-" + DateTime.UtcNow.Ticks,
                        Topic = line.Topic,
                        Text = line.Text,
                        Lap = snap.Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps ?? 0,
                    });
                }

                string category = line.Topic == "usure" ? ExchangeCategorizer.RaceSettings : ExchangeCategorizer.RaceInfo;
                await _engineer.SayAsync(line.Text, "ingénieur IA", ct, proactive: true, awaitingAnswer: line.IsQuestion, category: category, urgent: true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Alerte de pilotage impossible");
        }
    }
}

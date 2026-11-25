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
    private DateTime _lastWearAlert = DateTime.MinValue;

    /// <summary>Au plus une alerte d'usure anormale dans cet intervalle (le pilote en recevait une toutes les 30 secondes).</summary>
    private static readonly TimeSpan WearAlertInterval = TimeSpan.FromMinutes(4);

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
        DateTime nextWearPoint = DateTime.MinValue;
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

            // « Je te redis l'usure dans deux tours » : promesse faite après une usure anormale, tenue ici.
            // « Annonce-moi l'écart à chaque seconde » : demandé par le pilote, donc dit même sans interventions spontanées.
            if (DateTime.UtcNow >= nextWearPoint)
            {
                nextWearPoint = DateTime.UtcNow.AddSeconds(3);
                try
                {
                    RaceSnapshot snapNow = _engine.BuildSnapshot();
                    if (ai.DrivingAlerts && WearFollowUp.Check(snapNow, _engineer.Memory, DateTime.UtcNow) is string point)
                    {
                        await _engineer.SayAsync(point, "ingénieur IA", stoppingToken, proactive: true, category: ExchangeCategorizer.RaceSettings);
                    }

                    if (_engineer.Memory.GapWatch is GapWatch gaps && !Finished(snapNow) && GapWatcher.Check(gaps, snapNow, DateTime.UtcNow) is string gapLine)
                    {
                        await _engineer.SayAsync(gapLine, "ingénieur IA", stoppingToken, proactive: true, category: ExchangeCategorizer.RaceInfo);
                    }

                    // Essai de lift and coast demandé par le pilote : mesure à la fin du tour de test, puis suivi de la cible.
                    if (!Finished(snapNow) && LiftCoast.Check(_engineer.Memory, snapNow, _engine.PlayerLaps()) is RadioLine lift)
                    {
                        await _engineer.SayAsync(lift.Text, "ingénieur IA", stoppingToken, proactive: true, awaitingAnswer: lift.IsQuestion, category: ExchangeCategorizer.Strategy);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Point d'usure impossible");
                }
            }

            if (DateTime.UtcNow < nextRules)
            {
                continue;
            }

            nextRules = DateTime.UtcNow.AddSeconds(3);

            try
            {
                RaceSnapshot snap = _engine.BuildSnapshot();

                // Fin de la course, de la qualification ou des essais : le résultat, une fois, même sans interventions spontanées.
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

                // Le pilote est en pleine conversation : les interventions sans urgence attendent (les règles gardent leur tour).
                if (!ai.ProactiveRadio || _engineer.DriverTalking)
                {
                    continue;
                }

                // Un arrêt de moins à portée avec une petite économie : l'ingénieur propose un tour de lift and coast pour mesurer.
                if (!Finished(snap) && LiftCoast.Propose(_engineer.Memory, snap, DateTime.UtcNow) is RadioLine proposal)
                {
                    await _engineer.SayAsync(proposal.Text, "ingénieur IA", stoppingToken, proactive: true, awaitingAnswer: true, category: ExchangeCategorizer.Strategy);
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

    /// <summary>Course finie pour le joueur (drapeau à damier passé) : plus d'alertes ni de conseils de course.</summary>
    private static bool Finished(RaceSnapshot snap)
    {
        DriverRow? me = snap?.Drivers.FirstOrDefault(d => d.IsPlayer);
        return snap?.SessionPhase == "Checkered" || me?.FinishStatus is "Finished" or "Dnf" or "Dq";
    }

    /// <summary>Alertes de pilotage : une phrase courte, sans attendre l'intervalle des autres messages.</summary>
    private async Task SayDrivingAsync(List<DrivingEvent> events, CancellationToken ct)
    {
        try
        {
            RaceSnapshot snap = _engine.BuildSnapshot();
            DriverRow? player = snap.Drivers.FirstOrDefault(d => d.IsPlayer);

            // Après le drapeau à damier (tour de décélération) ou au stand : plus d'usure ni de limites à commenter.
            if (Finished(snap) || player?.InPits == true)
            {
                events.RemoveAll(e => e.Type is "usure" or "limites" or "tour-annulé");
            }

            // Usure anormale : une alerte à la fois, au plus toutes les 4 minutes, jamais tant que la précédente attend sa réponse.
            if (events.Any(e => e.Type == "usure"))
            {
                bool waiting = _engineer.Memory.Pending(90) is { Topic: "usure" };
                if (waiting || DateTime.UtcNow - _lastWearAlert < WearAlertInterval)
                {
                    events.RemoveAll(e => e.Type == "usure");
                }
                else
                {
                    DrivingEvent first = events.First(e => e.Type == "usure");
                    events.RemoveAll(e => e.Type == "usure" && e != first);
                    _lastWearAlert = DateTime.UtcNow;
                }
            }

            if (events.Count == 0)
            {
                return;
            }

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
                        Wheel = line.Spike?.Wheel,
                        Phase = line.Spike?.Phase,
                        Corner = line.Spike?.Corner,
                        Cause = line.Cause,
                        Proposal = line.Proposal,
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

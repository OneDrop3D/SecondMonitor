namespace SecondMonitor.Hub.Services;

using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Reprise de course après un redémarrage : sauvegarde régulière de l'état de course (à chaque tour bouclé du joueur,
/// au moins toutes les 30 s, et à la fermeture), et annonce radio quand une course est reprise.
/// La décision de reprendre est prise par le moteur au premier paquet du jeu (<see cref="RaceResumeKeeper"/>).
/// </summary>
public sealed class SessionResumeService : BackgroundService
{
    private readonly RaceResumeKeeper _keeper;
    private readonly SettingsStore _settings;
    private readonly ILogger<SessionResumeService> _logger;

    public SessionResumeService(RaceResumeKeeper keeper, SettingsStore settings, EngineerService engineer, ILogger<SessionResumeService> logger)
    {
        _keeper = keeper;
        _settings = settings;
        _logger = logger;
        keeper.Resumed += state =>
        {
            _logger.LogInformation("Course reprise : {Summary}", state.Summary());
            if (!_settings.Current.Ai.ProactiveRadio)
            {
                return;
            }

            // Une seule fois, quelques secondes après la reprise (le temps que les écrans se reconnectent).
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await engineer.SayAsync($"Données de course reprises : {state.Summary()}.", "ingénieur IA", CancellationToken.None, proactive: true, category: ExchangeCategorizer.RaceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Annonce de la reprise non dite");
                }
            });
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _keeper.Tick(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sauvegarde de la course impossible");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _keeper.SaveNow(DateTime.UtcNow); // fermeture : l'état le plus récent, pour reprendre exactement là
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sauvegarde de la course à la fermeture impossible");
        }

        await base.StopAsync(cancellationToken);
    }
}

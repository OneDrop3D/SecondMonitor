namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.DataModel.BasicProperties;
using SecondMonitor.DataModel.Snapshot;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Remote.Models;

/// <summary>Reprise de course après un redémarrage de SecondMonitor (retour du pilote, Le Mans, 15/11).</summary>
[TestFixture]
public class RepriseCourseTests
{
    private const double Dt = 0.1;
    private string _dir;
    private RaceResumeStore _store;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "smhub-reprise-" + Guid.NewGuid().ToString("N"));
        _store = new RaceResumeStore(Path.Combine(_dir, "reprise", "session.json"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    /// <summary>Premier lancement : 12 minutes de course, puis sauvegarde (comme à la fermeture de SecondMonitor).</summary>
    private (DemoRaceSimulator Sim, RaceEngine Engine, RaceMemory Memory, RaceResumeKeeper Keeper) FirstRun(double seconds = 12 * 60, bool enabled = true)
    {
        var sim = new DemoRaceSimulator(raceDurationSeconds: 3600);
        var engine = new RaceEngine(_dir);
        var memory = new RaceMemory();
        engine.SessionReset += memory.Clear; // comme EngineerService
        var keeper = new RaceResumeKeeper(engine, memory, _store, () => enabled) { CarnetId = () => "20261115-103012-circuit" };
        for (double t = 0; t < seconds; t += Dt)
        {
            engine.Process(sim.Step(Dt));
        }

        return (sim, engine, memory, keeper);
    }

    /// <summary>Relancement : nouveau moteur, nouvelle mémoire, la sauvegarde est proposée avant le premier paquet.</summary>
    private (RaceEngine Engine, RaceMemory Memory, RaceResumeKeeper Keeper, List<string> Events) Relaunch(bool enabled = true)
    {
        var engine = new RaceEngine(_dir);
        var memory = new RaceMemory();
        var events = new List<string>();
        engine.SessionReset += () => { memory.Clear(); events.Add("reset"); };
        var keeper = new RaceResumeKeeper(engine, memory, _store, () => enabled);
        keeper.Resumed += _ => events.Add("reprise");
        keeper.Declined += reason => events.Add("refus : " + reason);
        keeper.Start(DateTime.UtcNow);
        return (engine, memory, keeper, events);
    }

    private static void Feed(RaceEngine engine, DemoRaceSimulator sim, double seconds, Action<SimulatorDataSet> change = null)
    {
        for (double t = 0; t < seconds; t += Dt)
        {
            SimulatorDataSet data = sim.Step(Dt);
            change?.Invoke(data);
            engine.Process(data);
        }
    }

    [Test]
    public void MemeSession_ToursConsommationArretsEtMemoire_SontRepris()
    {
        var (sim, engine, memory, keeper) = FirstRun();
        memory.Add("pilote", "note", "L'arrière glisse en sortie du virage Porsche", 4);
        memory.PlannedStopLap = 9;
        RaceSnapshot before = engine.BuildSnapshot();
        int playerLaps = engine.PlayerLaps().Count;
        int opponentStops = before.Drivers.Sum(d => d.PitStops);
        Assert.That(playerLaps, Is.GreaterThan(3));
        Assert.That(keeper.SaveNow(DateTime.UtcNow), Is.True);
        Assert.That(_store.Exists, Is.True);

        var (engine2, memory2, keeper2, events) = Relaunch();
        Assert.That(engine2.ResumePending, Is.True);
        Feed(engine2, sim, 2); // le jeu, lui, a continué
        RaceSnapshot after = engine2.BuildSnapshot();

        Assert.That(events, Is.EqualTo(new[] { "reprise" }), "Reprise sans remise à zéro (le carnet ne doit pas être fermé)");
        Assert.That(engine2.PlayerLaps().Count, Is.GreaterThanOrEqualTo(playerLaps), "Tours du joueur conservés");
        Assert.That(after.Player.Fuel.PerLap, Is.EqualTo(3.2).Within(0.25), "Consommation au tour connue tout de suite");
        Assert.That(after.Player.Fuel.PerLapEstimated, Is.False);
        Assert.That(after.Drivers.Sum(d => d.PitStops), Is.GreaterThanOrEqualTo(opponentStops), "Arrêts des adversaires conservés");
        Assert.That(after.Drivers.First(d => d.IsPlayer).Stints, Is.Not.Empty);
        Assert.That(memory2.Entries.Select(e => e.Text), Does.Contain("L'arrière glisse en sortie du virage Porsche"));
        Assert.That(memory2.PlannedStopLap, Is.EqualTo(9));
        Assert.That(keeper2.TakeResumedCarnetId(), Is.EqualTo("20261115-103012-circuit"), "Le carnet de la course continue");
        Assert.That(keeper2.TakeResumedCarnetId(), Is.Null, "Donné une seule fois");

        // La course continue normalement : les tours bouclés après la reprise s'ajoutent à la suite, sans trou de numérotation.
        Feed(engine2, sim, 5 * 60);
        var laps = engine2.PlayerLaps().Select(l => l.Lap).ToList();
        Assert.That(laps, Is.Ordered.And.Unique);
        Assert.That(laps.Count, Is.GreaterThan(playerLaps));
        Assert.That(laps.Zip(laps.Skip(1), (a, b) => b - a).All(d => d == 1), Is.True, string.Join(",", laps));
    }

    [Test]
    public void AutreCircuit_Refuse_EtEfface()
    {
        var (sim, _, _, keeper) = FirstRun(4 * 60);
        keeper.SaveNow(DateTime.UtcNow);

        var (engine2, _, _, events) = Relaunch();
        Feed(engine2, sim, 1, d => d.SessionInfo.TrackInfo.TrackName = "Spa-Francorchamps");

        Assert.That(events[0], Does.StartWith("refus : autre circuit"));
        Assert.That(events, Does.Contain("reset"));
        Assert.That(engine2.PlayerLaps(), Is.Empty);
        Assert.That(_store.Exists, Is.False, "Sauvegarde effacée");
    }

    [Test]
    public void AutreTypeDeSession_Refuse()
    {
        var (sim, _, _, keeper) = FirstRun(4 * 60);
        keeper.SaveNow(DateTime.UtcNow);

        var (engine2, _, _, events) = Relaunch();
        Feed(engine2, sim, 1, d => d.SessionInfo.SessionType = SessionType.Practice);

        Assert.That(events[0], Does.StartWith("refus : autre type de session"));
        Assert.That(engine2.PlayerLaps(), Is.Empty);
        Assert.That(_store.Exists, Is.False);
    }

    [Test]
    public void TempsDeSessionRevenuEnArriere_Refuse()
    {
        var (_, _, _, keeper) = FirstRun(4 * 60);
        keeper.SaveNow(DateTime.UtcNow);

        // Même circuit, même type, mais la session a recommencé (course relancée dans le jeu).
        var (engine2, _, _, events) = Relaunch();
        Feed(engine2, new DemoRaceSimulator(raceDurationSeconds: 3600), 1);

        Assert.That(events[0], Does.StartWith("refus : temps de session revenu en arrière"));
        Assert.That(engine2.PlayerLaps(), Is.Empty);
        Assert.That(_store.Exists, Is.False);
    }

    [Test]
    public void SauvegardeTropAncienne_Refusee()
    {
        var (_, _, _, keeper) = FirstRun(2 * 60);
        keeper.SaveNow(DateTime.UtcNow - TimeSpan.FromHours(13));

        var (engine2, _, _, events) = Relaunch();
        Assert.That(engine2.ResumePending, Is.False);
        Assert.That(events[0], Does.StartWith("refus : sauvegarde trop ancienne"));
        Assert.That(_store.Exists, Is.False);
    }

    [Test]
    public void ReglageDesactive_RienNestSauvegarde_EtAncienneSauvegardeEffacee()
    {
        var (_, engine, _, keeper) = FirstRun(4 * 60, enabled: false);
        Assert.That(keeper.Tick(DateTime.UtcNow), Is.False);
        Assert.That(keeper.SaveNow(DateTime.UtcNow), Is.False);
        Assert.That(_store.Exists, Is.False);

        // Une sauvegarde laissée par un lancement précédent n'est pas reprise et disparaît.
        var on = new RaceResumeKeeper(engine, new RaceMemory(), _store, () => true);
        Assert.That(on.SaveNow(DateTime.UtcNow), Is.True);
        var (engine2, _, _, _) = Relaunch(enabled: false);
        Assert.That(engine2.ResumePending, Is.False);
        Assert.That(_store.Exists, Is.False);
    }

    [Test]
    public void NouvelleSession_EffaceLaSauvegarde()
    {
        var (_, engine, _, keeper) = FirstRun(3 * 60);
        Assert.That(keeper.SaveNow(DateTime.UtcNow), Is.True);
        engine.StartNewSession();
        Assert.That(_store.Exists, Is.False);
    }

    [Test]
    public void Tick_SauvegardeAuTourBoucle_EtAuPlusToutesLes30s()
    {
        var (sim, engine, _, keeper) = FirstRun(3 * 60);
        DateTime now = DateTime.UtcNow;
        Assert.That(keeper.Tick(now), Is.True, "Première sauvegarde");
        Assert.That(keeper.Tick(now.AddSeconds(5)), Is.False, "Rien de neuf");
        Assert.That(keeper.Tick(now.AddSeconds(31)), Is.True, "30 s écoulées");

        int lap = engine.BuildSnapshot().Drivers.First(d => d.IsPlayer).Laps;
        while (engine.BuildSnapshot().Drivers.First(d => d.IsPlayer).Laps == lap)
        {
            Feed(engine, sim, 1);
        }

        Assert.That(keeper.Tick(now.AddSeconds(32)), Is.True, "Tour bouclé : sauvegarde tout de suite");
    }

    [Test]
    public void DebutDeSessionRepete_EnPleineSession_NeRemetPasAZero()
    {
        // Le collecteur renvoie « début de session » quand il se remet à lire le jeu : le carnet était coupé en deux (103012 / 103015).
        var (sim, engine, _, _) = FirstRun(4 * 60);
        int resets = 0;
        engine.SessionReset += () => resets++;
        int laps = engine.PlayerLaps().Count;

        engine.Process(sim.Step(Dt), DatagramPayloadKind.SessionStart);
        Assert.That(resets, Is.Zero);
        Assert.That(engine.PlayerLaps().Count, Is.EqualTo(laps));

        // Une vraie nouvelle session (temps qui repart de zéro) remet toujours tout à zéro.
        engine.Process(new DemoRaceSimulator(raceDurationSeconds: 3600).Step(Dt), DatagramPayloadKind.SessionStart);
        Assert.That(resets, Is.EqualTo(1));
        Assert.That(engine.PlayerLaps(), Is.Empty);
    }

    [Test]
    public void Resume_DitToursEtArrets()
    {
        var state = new RaceResumeState { PlayerId = "moi", Drivers = { new DriverTrackerState { Id = "moi", Laps = Enumerable.Range(1, 12).Select(i => new LapRecord { Lap = i }).ToList(), PitStops = { new PitStopRecord { Lap = 8 } } } } };
        Assert.That(state.Summary(), Is.EqualTo("12 tours, 1 arrêt"));
        state.Drivers[0].PitStops.Clear();
        Assert.That(state.Summary(), Is.EqualTo("12 tours, aucun arrêt"));
    }
}

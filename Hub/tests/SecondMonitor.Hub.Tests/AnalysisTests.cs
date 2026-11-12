namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.DataModel.BasicProperties;
using SecondMonitor.DataModel.Snapshot;
using SecondMonitor.DataModel.Snapshot.Drivers;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Analyses détaillées : micro-secteurs, Vmax/Vmin, trajectoires, télémétrie, météo, duels, IA.</summary>
[TestFixture]
public class AnalysisTests
{
    private static RaceEngine _engine;
    private static RaceSnapshot _snap;
    private static string _dir;

    [OneTimeSetUp]
    public void RunRace()
    {
        _dir = Path.Combine(Path.GetTempPath(), "smhub-analysis-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator(seed: 11);
        _engine = new RaceEngine(_dir);
        for (double t = 0; t < 40 * 60; t += 0.1)
        {
            _engine.Process(sim.Step(0.1));
        }

        _snap = _engine.BuildSnapshot();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    [Test]
    public void MicroSectors_SumToLapTime()
    {
        MicroSectorReportDto ms = _engine.BuildMicroSectors();
        Assert.That(ms.Count, Is.EqualTo(DriverLapAnalysis.MicroSectorCount));
        DriverRow me = _snap.Drivers.First(d => d.IsPlayer);
        DriverMicroSectorsDto mine = ms.Drivers.First(d => d.Id == me.Id);
        Assert.That(mine.Best.All(v => v > 0), "Tous les micro-secteurs doivent avoir un meilleur temps");
        Assert.That(mine.TheoreticalBest, Is.LessThanOrEqualTo(me.BestLap + 0.3).And.GreaterThan(me.BestLap - 3));
        Assert.That(ms.ClassBest.Keys, Has.Count.EqualTo(2));
        foreach (var (cls, best) in ms.ClassBest)
        {
            Assert.That(best.All(v => v > 0), $"Meilleurs micro-secteurs incomplets pour {cls}");
        }
    }

    [Test]
    public void Speeds_FindCornersAndStraights()
    {
        SpeedAnalysisDto sp = _engine.BuildSpeedAnalysis();
        Assert.That(sp.Segments.Count(s => s.Kind == "corner"), Is.GreaterThanOrEqualTo(3));
        Assert.That(sp.Segments.Count(s => s.Kind == "straight"), Is.EqualTo(sp.Segments.Count(s => s.Kind == "corner")));
        Assert.That(sp.Segments.Count(s => s.IsMain), Is.EqualTo(1));

        DriverSpeedDto leader = sp.Drivers.First();
        foreach (TrackSegment seg in sp.Segments)
        {
            Assert.That(leader.Best.ContainsKey(seg.Id), $"Pas de mesure pour {seg.Id}");
        }

        double vmin = sp.Segments.Where(s => s.Kind == "corner").Max(s => leader.Best[s.Id]);
        double vmax = sp.Segments.Where(s => s.Kind == "straight").Min(s => leader.Best[s.Id]);
        Assert.That(vmin, Is.LessThan(vmax), "Une vitesse de virage dépasse une vitesse de ligne droite");
        Assert.That(leader.TopSpeedBest, Is.GreaterThan(150).And.LessThan(350));
    }

    [Test]
    public void Trajectories_And_SpeedTraces_AreServed()
    {
        SpeedAnalysisDto sp = _engine.BuildSpeedAnalysis();
        string[] ids = _snap.Drivers.Take(3).Select(d => d.Id).ToArray();
        TrajectoriesDto corner = _engine.BuildTrajectories(ids, sp.Segments.First(s => s.Kind == "corner").Id, best: true);
        Assert.That(corner.Drivers, Has.Count.EqualTo(3));
        Assert.That(corner.Drivers.All(d => d.Points.Count > 10));

        TrajectoriesDto lap = _engine.BuildTrajectories(ids.Take(1), null, best: false);
        Assert.That(lap.Drivers[0].Points.Count, Is.GreaterThan(200));

        SpeedTracesDto traces = _engine.BuildSpeedTraces(ids, best: true);
        Assert.That(traces.Drivers, Has.Count.EqualTo(3));
        Assert.That(traces.Drivers[0].Speed.Count(v => v.HasValue), Is.GreaterThan(traces.Drivers[0].Speed.Count * 0.9));
    }

    [Test]
    public void Telemetry_History_IsIncremental()
    {
        var all = _engine.ReadTelemetry(double.NegativeInfinity);
        Assert.That(all.T.Count, Is.GreaterThan(1000));
        Assert.That(all.Series.Keys, Does.Contain("brakeFL").And.Contain("coreRR").And.Contain("water").And.Contain("oil").And.Contain("track"));
        var tail = _engine.ReadTelemetry(all.T[^10]);
        Assert.That(tail.T, Has.Count.EqualTo(9));
    }

    [Test]
    public void Weather_HasHistoryAndTrend()
    {
        var w = _engine.BuildWeather();
        Assert.That(w.History.Count, Is.GreaterThan(100));
        Assert.That(w.Forecast, Has.Count.EqualTo(3));
        Assert.That(w.Summary, Does.StartWith("Tendance"));
        Assert.That(w.History.Any(h => h.Rain > 0), "L'averse de la démo (35e minute) doit apparaître");
    }

    [Test]
    public void Player_HasEngineBrakesAndBattles()
    {
        PlayerStrategy p = _snap.Player;
        Assert.That(p.Engine.WaterTemp, Is.GreaterThan(60));
        Assert.That(p.Engine.OilTemp, Is.GreaterThan(60));
        Assert.That(p.Wheels.All(w => w.Brake > 50 && w.Core > 30));
        Assert.That(p.Battles, Is.Not.Empty);
        Assert.That(_snap.DataRate.PlayerHz, Is.GreaterThan(5));
        Assert.That(_engine.PlayerLaps().Count(l => l.WearDelta != null), Is.GreaterThan(5));
    }

    [Test]
    public void LapFits_ProjectStint()
    {
        var laps = _engine.AllLaps();
        Assert.That(laps, Has.Count.EqualTo(16));
        Assert.That(laps.Count(l => l.Fit != null), Is.GreaterThan(3));
        Assert.That(laps.All(l => l.Laps.Count > 10));
    }

    [TestCase("Température des freins ?", RaceBriefing.Topic.Car)]
    [TestCase("Comment va le moteur ?", RaceBriefing.Topic.Car)]
    [TestCase("Et la carcasse des pneus ?", RaceBriefing.Topic.Tyres)]
    [TestCase("Il reste beaucoup de tours d'essence ?", RaceBriefing.Topic.Fuel)]
    public void Briefing_DetectsCarTopics(string question, RaceBriefing.Topic expected)
    {
        Assert.That(RaceBriefing.DetectTopics(question).First(), Is.EqualTo(expected));
    }

    [Test]
    public void Briefing_AnswersAboutBrakesAndEngine()
    {
        BriefingResult brakes = RaceBriefing.Prepare(_snap, "Température des freins ?");
        Assert.That(brakes.Suggestion, Does.StartWith("Freins :"));
        Assert.That(brakes.Facts, Does.Contain("Moteur : eau"));
        Assert.That(brakes.Facts, Does.Contain("Carcasse des pneus"));
    }

    [Test]
    public void TestScenario_GivesARaceForTheAi()
    {
        RaceSnapshot snap = TestScenario.Get();
        Assert.That(snap.HasData);
        Assert.That(snap.Player, Is.Not.Null);
        Assert.That(RaceBriefing.Prepare(snap, "Je rentre quand ?").Suggestion, Is.Not.Null);
    }

    /// <summary>
    /// Le PC n'envoie les adversaires qu'une fois par seconde : entre deux envois, le hub reçoit les mêmes objets.
    /// Ils doivent être ignorés (sinon les voitures semblent immobiles puis téléportées) et le joueur, envoyé 5×/s, utilisé à jour.
    /// </summary>
    [Test]
    public void StaleOpponents_AreIgnored_FreshPlayerIsUsed()
    {
        var engine = new RaceEngine(Path.Combine(_dir, "stale"));
        var sim = new DemoRaceSimulator(seed: 5);
        SimulatorDataSet full = null;
        for (int i = 0; i < 3000; i++)
        {
            SimulatorDataSet data = sim.Step(0.2);
            if (i % 5 == 0)
            {
                full = data;
            }
            else
            {
                // Envoi « économe » : liste des pilotes de l'envoi précédent, joueur à jour.
                DriverInfo freshPlayer = data.PlayerInfo;
                data.DriversInfo = full.DriversInfo;
                data.PlayerInfo = freshPlayer;
            }

            engine.Process(data);
        }

        RaceSnapshot snap = engine.BuildSnapshot();
        Assert.That(snap.DataRate.PlayerHz, Is.GreaterThan(4), "Le joueur doit être suivi à pleine fréquence");
        Assert.That(snap.DataRate.OpponentsHz, Is.LessThan(1.5).And.GreaterThan(0.5));
        Assert.That(snap.Capabilities, Does.Contain("lowRateOpponents"));
        foreach (DriverRow d in snap.Drivers.Skip(1).Where(d => d.Interval.HasValue && d.IntervalLaps == 0))
        {
            Assert.That(d.Interval, Is.GreaterThanOrEqualTo(0).And.LessThan(200));
        }
    }
}

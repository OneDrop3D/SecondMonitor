namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.DataModel.BasicProperties;
using SecondMonitor.DataModel.Snapshot.Drivers;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Historique des micro-secteurs par tour, comparaison des tours du joueur (rapports, réglages en roulant, delta), trajectoires.</summary>
[TestFixture]
public class LapComparisonTests
{
    private static RaceEngine _engine;
    private static RaceSnapshot _snap;
    private static string _dir;

    [OneTimeSetUp]
    public void RunRace()
    {
        _dir = Path.Combine(Path.GetTempPath(), "smhub-lapcompare-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator(seed: 7);
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
    public void MicroSectorLaps_SumToLapTime_WithCompoundAndTyreAge()
    {
        MicroSectorLapsDto all = _engine.BuildMicroSectorLaps(Array.Empty<string>());
        Assert.That(all.Count, Is.EqualTo(DriverLapAnalysis.MicroSectorCount));
        Assert.That(all.Drivers, Has.Count.EqualTo(_snap.Drivers.Count));

        int checkedLaps = 0;
        foreach (DriverMicroSectorLapsDto d in all.Drivers)
        {
            Assert.That(d.Name, Is.Not.Empty);
            foreach (MicroSectorLapDto lap in d.Laps.Where(l => l.Flags == 0 && l.Time > 0 && l.Ms != null && l.Ms.All(v => v.HasValue)))
            {
                // Même numérotation que les tours chronométrés : la somme des 24 micro-secteurs redonne le temps du tour.
                Assert.That(lap.Ms.Sum(v => v.Value), Is.EqualTo(lap.Time.Value).Within(0.5), $"{d.Name} tour {lap.Lap}");
                Assert.That(lap.Compound, Is.Not.Null.And.Not.Empty);
                Assert.That(lap.TyreLaps, Is.GreaterThanOrEqualTo(1));
                checkedLaps++;
            }
        }

        Assert.That(checkedLaps, Is.GreaterThan(50));

        // Après un arrêt : nouveau relais, pneus neufs (âge 1 au tour de sortie).
        DriverMicroSectorLapsDto stopped = all.Drivers.First(d => d.Laps.Any(l => l.Stint >= 2));
        MicroSectorLapDto outLap = stopped.Laps.First(l => l.Stint == 2);
        Assert.That(outLap.TyreLaps, Is.EqualTo(1));
        Assert.That(outLap.Flags & 2, Is.EqualTo(2), "le premier tour du relais est un tour de sortie");
        MicroSectorLapDto before = stopped.Laps.Last(l => l.Stint == 1);
        Assert.That(before.TyreLaps, Is.GreaterThan(3));
    }

    [Test]
    public void MicroSectorLaps_CanBeFilteredById()
    {
        string id = _snap.Drivers.First(d => d.IsPlayer).Id;
        MicroSectorLapsDto mine = _engine.BuildMicroSectorLaps(new[] { id });
        Assert.That(mine.Drivers.Select(d => d.Id), Is.EqualTo(new[] { id }));
        Assert.That(mine.Drivers[0].IsPlayer);
        Assert.That(mine.LapLength, Is.GreaterThan(1000));
    }

    [Test]
    public void LapComparison_ResolvesBestAndLast_WithGearsAndDelta()
    {
        LapComparisonDto cmp = _engine.BuildLapComparison(new[] { "best", "last" });
        Assert.That(cmp.Step, Is.EqualTo(DrivingWatch.BinMeters));
        Assert.That(cmp.Laps.Count(l => l.Best), Is.EqualTo(1));
        LapChoiceDto best = cmp.Laps.Single(l => l.Best);
        Assert.That(cmp.Traces[0].Lap, Is.EqualTo(best.Lap));
        Assert.That(cmp.Traces.Select(t => t.Lap), Is.Unique);

        LapDetailDto lap = cmp.Traces[0];
        Assert.That(lap.Time, Is.EqualTo(best.Time));
        Assert.That(lap.Compound, Is.Not.Null);
        Assert.That(lap.Gear.Count(g => g >= 1), Is.GreaterThan(lap.Gear.Count * 0.6));
        Assert.That(lap.Shifts.Count(s => s.To > s.From), Is.GreaterThan(2), "montées de rapport");
        Assert.That(lap.Shifts.Count(s => s.To < s.From), Is.GreaterThan(2), "descentes de rapport");
        Assert.That(lap.Shifts.All(s => Math.Abs(s.To - s.From) == 1));
        Assert.That(lap.Controls["bias"], Is.EqualTo(54.5));
        Assert.That(lap.Controls["tc"], Is.EqualTo(4));

        // Temps écoulé depuis la ligne : croissant, et le dernier point tombe sur le temps du tour.
        var elapsed = lap.Elapsed.Where(v => v.HasValue).Select(v => v.Value).ToList();
        Assert.That(elapsed, Is.Ordered);
        Assert.That(elapsed[^1], Is.EqualTo(lap.Time.Value).Within(1.5));
    }

    [Test]
    public void LapComparison_KnowsCurrentLapAndIgnoresUnknownLaps()
    {
        LapComparisonDto cmp = _engine.BuildLapComparison(new[] { "current", "999", "abc" });
        Assert.That(cmp.Traces, Has.Count.EqualTo(1));
        Assert.That(cmp.Traces[0].Current, Is.True);
        Assert.That(cmp.Laps.Last().Current, Is.True);
    }

    [Test]
    public void Trajectories_CarryDistance_AtFullResolution()
    {
        string id = _snap.Drivers.First(d => d.IsPlayer).Id;
        TrajectoriesDto lap = _engine.BuildTrajectories(new[] { id }, null, best: true);
        List<double[]> points = lap.Drivers[0].Points;
        Assert.That(points.All(p => p.Length == 4));
        Assert.That(points.Select(p => p[3]), Is.Ordered.And.Unique);
        double step = points.Skip(1).Zip(points, (a, b) => a[3] - b[3]).Min();
        Assert.That(step, Is.LessThan(DriverLapAnalysis.TargetBinMeters * 1.5), "une position tous les ~10 m");
    }

    [Test]
    public void DrivingWatch_FindsShiftsAndControlChanges()
    {
        var watch = new DrivingWatch();
        const double lapLength = 2000;
        const double speedMs = 40;
        for (double t = 0; t < 3 * lapLength / speedMs; t += 0.1)
        {
            double total = speedMs * t;
            int lap = (int)(total / lapLength);
            double dist = total - (lap * lapLength);
            string gear = dist < 500 ? "3" : dist < 1200 ? "4" : dist < 1500 ? "3" : "1";
            // Répartition baissée en deux crans rapprochés vers 800 m (un seul changement), remise à 56 sur la ligne (réglage de départ du tour suivant).
            double bias = dist < 800 ? 56.0 : dist < 812 ? 55.8 : 55.5;
            watch.Update(t, Car(lap, dist, speedMs * 3.6, gear, 6000 + (int)(dist % 500), bias), new InputInfo { ThrottlePedalPosition = 1 }, lapLength, null, "Practice", "Green");
        }

        LapDetailDto second = watch.LapDetail(2);
        Assert.That(second, Is.Not.Null);
        Assert.That(second.Complete, Is.True);

        // 1 → 3 juste après la ligne (deux rapports d'un coup entre deux mesures : un passage pour chacun, au même endroit).
        var shifts = second.Shifts.Select(s => (s.From, s.To, Math.Round(s.Distance / 10) * 10)).ToList();
        Assert.That(shifts, Is.EqualTo(new[] { (1, 2, 0.0), (2, 3, 0.0), (3, 4, 500.0), (4, 3, 1200.0), (3, 2, 1500.0), (2, 1, 1500.0) }));
        Assert.That(second.Shifts[2].Speed, Is.EqualTo(144).Within(0.5));
        Assert.That(second.Shifts[2].RpmBefore, Is.GreaterThan(6400), "régime juste avant la montée");

        Assert.That(second.Controls["bias"], Is.EqualTo(56.0), "réglage au départ du tour");
        Assert.That(second.ControlChanges, Has.Count.EqualTo(1), "les deux crans rapprochés ne font qu'un changement ; rien sur la ligne");
        ControlChangeDto change = second.ControlChanges[0];
        Assert.That((change.Key, change.From, change.To), Is.EqualTo(("bias", 56.0, 55.5)));
        Assert.That(change.Distance, Is.EqualTo(800).Within(10));

        // Temps écoulé depuis la ligne : 2000 m à 40 m/s = 50 s.
        double last = second.Elapsed.Last(v => v.HasValue).Value;
        Assert.That(last, Is.EqualTo(50).Within(0.3));
        Assert.That(second.Elapsed.First(v => v.HasValue).Value, Is.EqualTo(0.125).Within(0.2));
        Assert.That(second.Gear[100], Is.EqualTo(4));

        var laps = watch.RecordedLaps();
        Assert.That(laps.Select(l => l.Lap), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(laps[^1].Current, Is.True);
    }

    private static DriverInfo Car(int completedLaps, double dist, double kph, string gear, int rpm, double bias)
    {
        var d = new DriverInfo
        {
            IsPlayer = true,
            DriverSessionId = "1",
            CompletedLaps = completedLaps,
            LapDistance = dist,
            Speed = Velocity.FromKph(kph),
            CarInfo = new SecondMonitor.DataModel.Snapshot.Systems.CarInfo(),
        };
        d.CarInfo.EngineRpm = rpm;
        d.CarInfo.CurrentGear = gear;
        d.CarInfo.Controls.Available = true;
        d.CarInfo.Controls.FrontBrakeBiasPct = bias;
        d.CarInfo.Controls.Tc = 5;
        return d;
    }
}

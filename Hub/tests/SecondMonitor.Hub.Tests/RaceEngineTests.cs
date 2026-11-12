namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

[TestFixture]
public class RaceEngineTests
{
    private string _dir;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "smhub-tests-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private RaceSnapshot Run(double seconds, double dt = 0.1)
    {
        var sim = new DemoRaceSimulator(raceDurationSeconds: 3600);
        var engine = new RaceEngine(_dir);
        for (double t = 0; t < seconds; t += dt)
        {
            engine.Process(sim.Step(dt));
        }

        return engine.BuildSnapshot();
    }

    [Test]
    public void Gaps_AreConsistent_WithClassification()
    {
        RaceSnapshot snap = Run(20 * 60);
        Assert.That(snap.Drivers, Has.Count.EqualTo(16));
        Assert.That(snap.Drivers[0].GapToLeader, Is.EqualTo(0));

        var onLeadLap = snap.Drivers.Where(d => d.GapToLeaderLaps == 0 && d.GapToLeader.HasValue).ToList();
        Assert.That(onLeadLap.Count, Is.GreaterThan(3));
        for (int i = 1; i < onLeadLap.Count; i++)
        {
            Assert.That(onLeadLap[i].GapToLeader, Is.GreaterThanOrEqualTo(onLeadLap[i - 1].GapToLeader - 0.5), $"Écart au leader non croissant en P{onLeadLap[i].Position}");
        }

        foreach (DriverRow d in snap.Drivers.Skip(1).Where(d => d.Interval.HasValue))
        {
            Assert.That(d.Interval, Is.GreaterThanOrEqualTo(0).And.LessThan(200));
        }
    }

    [Test]
    public void PitStops_And_Stints_AreTracked()
    {
        RaceSnapshot snap = Run(40 * 60);
        Assert.That(snap.Drivers.Sum(d => d.PitStops), Is.GreaterThan(5), "Les arrêts au stand devraient être détectés");
        DriverRow withStop = snap.Drivers.First(d => d.PitStops > 0);
        Assert.That(withStop.Stints.Count, Is.EqualTo(withStop.PitStops + 1));
        Assert.That(withStop.LastPitDuration, Is.InRange(25, 45));
        Assert.That(snap.Drivers.Count(d => d.NextStop != null), Is.GreaterThan(5));
    }

    [Test]
    public void PlayerStrategy_LearnsConsumption()
    {
        RaceSnapshot snap = Run(12 * 60);
        PlayerStrategy p = snap.Player;
        Assert.That(p, Is.Not.Null);
        Assert.That(p.Fuel.PerLap, Is.EqualTo(3.2).Within(0.25));
        Assert.That(p.Energy, Is.Not.Null);
        Assert.That(p.Energy.PerLapPct, Is.GreaterThan(5).And.LessThan(12));
        Assert.That(p.Wheels, Has.Count.EqualTo(4));
        Assert.That(p.Wheels[0].WearPerLapPct, Is.GreaterThan(1).And.LessThan(8));
        Assert.That(p.LapsRemaining, Is.GreaterThan(10));
        Assert.That(p.RecommendedStopLap, Is.Not.Null);
    }

    [Test]
    public void TrackMap_IsLearned_AfterFullLap()
    {
        RaceSnapshot snap = Run(6 * 60);
        Assert.That(snap.Track.Outline, Is.Not.Null);
        Assert.That(snap.Track.Outline, Has.Count.EqualTo(400));
    }
}

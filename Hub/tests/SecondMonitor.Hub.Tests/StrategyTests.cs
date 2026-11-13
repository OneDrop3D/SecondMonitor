namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.DataModel.Snapshot.Drivers;
using SecondMonitor.DataModel.Snapshot.Systems;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

[TestFixture]
public class StrategyTests
{
    private static RaceSnapshot _snap;
    private static RaceEngine _engine;

    [OneTimeSetUp]
    public void RunLongRace()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smhub-strat-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator();
        _engine = new RaceEngine(dir);
        for (double t = 0; t < 40 * 60; t += 0.1)
        {
            _engine.Process(sim.Step(0.1));
        }

        _snap = _engine.BuildSnapshot();
    }

    [Test]
    public void PitPlan_IsComputedInRace_WithMeasuredLossAndRejoin()
    {
        var plan = _snap.Player.PitPlan;
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.PitLoss, Is.InRange(8, 120));
        Assert.That(plan.RejoinNow.Text, Does.StartWith("Si tu rentres maintenant, tu ressors P"));
        TestContext.Out.WriteLine($"Perte {plan.PitLoss} s ({plan.PitLossSource}), pneus neufs {plan.FreshTyreGain} ({plan.FreshTyreGainSource})");
        TestContext.Out.WriteLine(plan.RejoinNow.Text);
        foreach (var c in plan.Cuts)
        {
            TestContext.Out.WriteLine($"{c.Kind} {c.Feasible} : {c.Text}");
        }
    }

    [Test]
    public void Ai_AnswersUndercutAndRejoinQuestions()
    {
        var r = RaceBriefing.Prepare(_snap, "Si je rentre maintenant, je ressors où ?");
        Assert.That(r.Suggestion, Does.Contain("ressortez P").Or.Contain("ndercut"));
        var u = RaceBriefing.Prepare(_snap, "Undercut possible sur la voiture devant ?");
        TestContext.Out.WriteLine(u.Suggestion);
        Assert.That(u.Topics.First(), Is.EqualTo(RaceBriefing.Topic.Strategy));
    }

    [Test]
    public void Damage_NewHit_IsDetected_ThenItsCostMeasured()
    {
        var watch = new DamageWatch();
        var player = new DriverInfo { CarInfo = new CarInfo() };
        var laps = new List<(int, double)> { (1, 100.0), (2, 100.2), (3, 99.9) };
        watch.Update(player, 300, 3, laps);
        Assert.That(watch.LastEvent, Is.Null);

        player.CarInfo.CarDamageInformation.Parts.Add(new DamagePart("front-wing-left", "aero", 0.25));
        var parts = watch.Update(player, 310, 3, laps);
        Assert.That(parts.Single(p => p.Key == "front-wing-left").Label, Is.EqualTo("aileron avant gauche"));
        Assert.That(watch.LastEvent.Percent, Is.EqualTo(25));

        laps.AddRange(new[] { (4, 100.9), (5, 100.7), (6, 100.8) });
        watch.Update(player, 700, 6, laps);
        Assert.That(watch.LastEvent.LossPerLap, Is.EqualTo(0.75).Within(0.11));
    }
}

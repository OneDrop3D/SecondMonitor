namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

[TestFixture]
public class BriefingTests
{
    private static RaceSnapshot _snap;

    [OneTimeSetUp]
    public void RunRace()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smhub-brief-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator();
        var engine = new RaceEngine(dir);
        for (double t = 0; t < 15 * 60; t += 0.1)
        {
            engine.Process(sim.Step(0.1));
        }

        _snap = engine.BuildSnapshot();
        Directory.Delete(dir, true);
    }

    [TestCase("Je rentre quand ?", RaceBriefing.Topic.Pit)]
    [TestCase("Box ce tour ?", RaceBriefing.Topic.Pit)]
    [TestCase("Il me reste combien de tours d'essence ?", RaceBriefing.Topic.Fuel)]
    [TestCase("C'est combien l'écart avec la voiture derrière ?", RaceBriefing.Topic.Gap)]
    [TestCase("Quelle est la stratégie de la voiture derrière ?", RaceBriefing.Topic.Opponents)]
    [TestCase("Comment vont mes pneus ?", RaceBriefing.Topic.Tyres)]
    public void DetectsMainTopic(string question, RaceBriefing.Topic expected)
    {
        Assert.That(RaceBriefing.DetectTopics(question).First(), Is.EqualTo(expected));
    }

    [Test]
    public void Suggestion_ForCarBehind_TalksAboutCarBehind()
    {
        BriefingResult r = RaceBriefing.Prepare(_snap, "Écart avec la voiture derrière ?");
        Assert.That(r.Suggestion, Does.Contain("derrière"));
        Assert.That(r.Suggestion, Does.Not.Contain("devant vous,"));
    }

    [Test]
    public void Suggestion_ForFuel_GivesLapsOfFuel()
    {
        BriefingResult r = RaceBriefing.Prepare(_snap, "Combien de tours d'essence ?");
        Assert.That(r.Suggestion, Does.Contain("tours d'essence"));
    }

    [Test]
    public void FreeQuestion_HasFactsButNoSuggestion()
    {
        BriefingResult r = RaceBriefing.Prepare(_snap, "Raconte-moi une blague");
        Assert.That(r.Suggestion, Is.Null);
        Assert.That(r.Facts, Does.Contain("Vous êtes P"));
    }

    [Test]
    public void NextStopFuel_NeverExceedsTank()
    {
        var fuel = _snap.Player.Fuel;
        if (fuel.NextStopAdd.HasValue)
        {
            Assert.That(fuel.NextStopAdd.Value, Is.LessThanOrEqualTo(fuel.Capacity + 0.01));
        }
    }
}

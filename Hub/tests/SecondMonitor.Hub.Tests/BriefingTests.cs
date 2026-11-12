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
    [TestCase("Quel rythme dois-je faire pour gagner la course ?", RaceBriefing.Topic.Pace)]
    [TestCase("Voilà, j'aurais quoi comme rythme, à peu près ?", RaceBriefing.Topic.Pace)]
    [TestCase("Comment la météo va-t-elle évoluer durant la course ?", RaceBriefing.Topic.Weather)]
    [TestCase("La piste va évoluer durant toute la course", RaceBriefing.Topic.Weather)]
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

    [Test]
    public void Pace_IsComputed_WithTargetToCatchOrToStayAhead()
    {
        BriefingResult r = RaceBriefing.Prepare(_snap, "Quel rythme dois-je faire pour gagner la course ?");
        Assert.That(r.Suggestion, Does.Contain("Votre rythme est de"));
        Assert.That(r.Suggestion, Does.Contain("minute"));
        Assert.That(r.Facts, Does.Contain("Pour rattraper").Or.Contain("Vous menez"));
    }

    [Test]
    public void SeveralQuestions_AreAllAnswered()
    {
        BriefingResult r = RaceBriefing.Prepare(_snap, "Quelle est ma position, quelle est la météo, quelles sont les prévisions météo pour la course ?");
        Assert.That(r.Suggestion, Does.Contain("Vous êtes P"));
        Assert.That(r.Suggestion, Does.Contain("Air "));
    }

    [TestCase("La piste va évoluer durant toute la course", true)]
    [TestCase("J'aurais quoi comme rythme à peu près", true)]
    [TestCase("Je dois tourner en combien pour gagner", true)]
    [TestCase("L'arrière glisse beaucoup", false)]
    public void Dictation_WithoutQuestionMark_IsStillAQuestion(string text, bool question)
    {
        Assert.That(RadioCommands.IsQuestion(text), Is.EqualTo(question));
        if (question)
        {
            Assert.That(RadioCommands.IsFeedback(text), Is.False);
        }
    }

    [Test]
    public void TyreWatch_FlagsAWholeLapAboveRange()
    {
        var watch = new TyreThermalWatch();
        var wheel = new WheelStatus { Position = "FL", TempInner = 104, TempMiddle = 102, TempOuter = 100, IdealTemp = 90, TempWindow = 5 };
        List<TyreThermalDto> t = new();
        for (double now = 0; now <= 60; now += 0.5)
        {
            t = watch.Update(now, 0, true, new[] { wheel });
        }

        t = watch.Update(60.5, 1, true, new[] { wheel });
        Assert.That(t[0].LastLapState, Is.EqualTo("chaud"));
        Assert.That(t[0].HotSeconds, Is.GreaterThanOrEqualTo(30));
        Assert.That(TyreThermalWatch.Alerts(t)[0], Does.Contain("tout le dernier tour"));
    }
}

namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.DataModel.BasicProperties;
using SecondMonitor.DataModel.Snapshot.Drivers;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Ingénieur 0.7 : réglages en roulant, mots seuls, demandes d'arrêt, « Copy », alertes de pilotage, usure brutale.</summary>
[TestFixture]
public class IngenieurTests
{
    private static RaceEngine _engine;
    private static RaceSnapshot _snap;
    private static string _dir;

    [OneTimeSetUp]
    public void RunRace()
    {
        _dir = Path.Combine(Path.GetTempPath(), "smhub-ingenieur-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator(seed: 7);
        _engine = new RaceEngine(_dir);
        for (double t = 0; t < 12 * 60; t += 0.1)
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

    [TestCase("Quelle est la cartographie moteur ?", RaceBriefing.Topic.Controls)]
    [TestCase("TC ?", RaceBriefing.Topic.Controls)]
    [TestCase("Et la balance des freins.", RaceBriefing.Topic.Controls)]
    [TestCase("Fure des pneus.", RaceBriefing.Topic.Tyres)]
    [TestCase("Pneus", RaceBriefing.Topic.Tyres)]
    [TestCase("Merci au niveau des tracks limits.", RaceBriefing.Topic.Rules)]
    [TestCase("Est-ce que tu as des données sur le mode d'énergie déployé ?", RaceBriefing.Topic.Energy)]
    [TestCase("Peux-tu analyser mon pilotage ?", RaceBriefing.Topic.Driving)]
    [TestCase("Température des freins ?", RaceBriefing.Topic.Car)]
    public void Topics_ShortAndMisheardQuestions(string question, RaceBriefing.Topic expected)
    {
        Assert.That(RaceBriefing.DetectTopics(question).First(), Is.EqualTo(expected));
    }

    [TestCase("Fure des pneus.", true)]
    [TestCase("Et la balance des freins.", true)]
    [TestCase("Pneus", true)]
    [TestCase("Les pneus glissent", false)]
    [TestCase("Je bloque l'avant au virage 1", false)]
    [TestCase("Le volant vibre.", false)]
    public void ShortRequests_AreQuestions(string text, bool expected)
    {
        Assert.That(RaceBriefing.IsShortRequest(text), Is.EqualTo(expected));
    }

    [Test]
    public void Controls_AreReadFromTheGame()
    {
        Assert.That(_snap.Player.Controls, Is.Not.Null);
        Assert.That(_snap.Player.Inputs, Is.Not.Null);
        Assert.That(RaceBriefing.Prepare(_snap, "Quelle est la cartographie moteur ?").Suggestion, Does.StartWith("Cartographie 2 sur 4"));
        Assert.That(RaceBriefing.Prepare(_snap, "TC ?").Suggestion, Does.StartWith("TC 4 sur 11, coupure 3"));
        Assert.That(RaceBriefing.Prepare(_snap, "Et la balance des freins.").Suggestion, Does.StartWith("Répartition 54,5 % sur l'avant, migration 3"));
        Assert.That(RaceBriefing.Prepare(_snap, "Quel est mon ABS ?").Suggestion, Does.Contain("Pas d'ABS"));
    }

    [Test]
    public void Tyres_AreConciseFourWheels()
    {
        string temps = RaceBriefing.Prepare(_snap, "Quelle est la température des pneus ?").Suggestion;
        Assert.That(temps, Does.Match(@"^Avant gauche \d+ degrés, avant droit \d+, arrière gauche \d+, arrière droit \d+\.$"));
        string wear = RaceBriefing.Prepare(_snap, "Fure des pneus").Suggestion;
        Assert.That(wear, Does.StartWith("Usure : avant gauche"));
        Assert.That(wear, Does.Not.Contain("Trop froid"));
    }

    [Test]
    public void Driving_HasPedalSummaryAfterALap()
    {
        Assert.That(_engine.DrivingSummary(), Does.StartWith("Pilotage du tour"));
        InputTraceDto trace = _engine.InputTrace("last");
        Assert.That(trace, Is.Not.Null);
        Assert.That(trace.Throttle.Count(v => v.HasValue), Is.GreaterThan(trace.Throttle.Count / 2));
        Assert.That(trace.Brake.Any(v => v > 20), "Il doit y avoir des freinages");
    }

    [Test]
    public void CarKnowledge_KnowsCategories()
    {
        Assert.That(CarKnowledge.Category(new CarContext { CarClass = "Hypercar", CarName = "Porsche 963", Game = "LMU" }), Is.EqualTo("Hypercar LMDh"));
        Assert.That(CarKnowledge.Category(new CarContext { CarClass = "Hypercar", CarName = "Toyota GR010", Game = "LMU" }), Is.EqualTo("Hypercar LMH"));
        Assert.That(CarKnowledge.Category(new CarContext { CarClass = "GT3", CarName = "Ferrari 296", Game = "LMU" }), Is.EqualTo("LMGT3"));
        Assert.That(CarKnowledge.HowItWorks("Hypercar LMH"), Does.Contain("190 km/h"));
    }

    [TestCase("sortie", "RR", "TC de 4 à 5")]
    [TestCase("freinage", "FL", "répartition de 54,5 à 54,0 %")]
    [TestCase("entrée", "RL", "migration de 3 à 2")]
    public void WearSpike_GivesASettingWithCurrentValues(string phase, string wheel, string expected)
    {
        var controls = new ControlsDto { Tc = 4, TcMax = 11, FrontBrakeBiasPct = 54.5, Migration = 3, MigrationMax = 6 };
        var (_, proposal) = CarKnowledge.AnalyseWear(new WearSpike { Phase = phase, Wheel = wheel, DeltaPct = 2.5, Seconds = 3 }, controls, new CarContext { CarClass = "Hypercar" });
        Assert.That(proposal, Is.EqualTo(expected));
    }

    [Test]
    public void TcAtMax_SaysSo()
    {
        var controls = new ControlsDto { Tc = 11, TcMax = 11 };
        Assert.That(CarKnowledge.TcUp(controls, new CarContext()), Does.StartWith("TC déjà au maximum"));
    }

    [TestCase("L'équipe, je boxe, je boxe.", true, null, false)]
    [TestCase("Je veux des nouveaux pneus pour, euh, quand je vais boxer.", false, "neufs", false)]
    [TestCase("Moi je haussile le pneu, je veux des nouveaux softs.", false, "Soft", false)]
    [TestCase("Je veux aussi la réparation de la voiture.", false, null, true)]
    [TestCase("On fait rachat de la voiture et des pneus.", false, "neufs", true)]
    public void PitOrders_AreUnderstood(string text, bool box, string tyres, bool repair)
    {
        PitOrder o = PitOrders.Parse(text);
        Assert.That(o, Is.Not.Null);
        Assert.That(o.BoxNow, Is.EqualTo(box));
        Assert.That(o.Tyres, Is.EqualTo(tyres));
        Assert.That(o.Repair, Is.EqualTo(repair));
    }

    [TestCase("Je rentre quand ?")]
    [TestCase("Quelle est l'usure des pneus ?")]
    [TestCase("Les pneus glissent en sortie")]
    public void PitOrders_IgnoreQuestionsAndFeedback(string text)
    {
        Assert.That(PitOrders.Parse(text), Is.Null);
    }

    [Test]
    public void PitOrders_MergeIntoOnePlan()
    {
        var memory = new RaceMemory();
        PitOrders.Confirm(PitOrders.Parse("je veux des softs neufs"), memory, _snap, 5);
        string reply = PitOrders.Confirm(PitOrders.Parse("je veux aussi la réparation"), memory, _snap, 5);
        Assert.That(reply, Does.StartWith("Copy"));
        Assert.That(reply, Does.Contain("Soft neufs"));
        Assert.That(reply, Does.Contain("réparation"));
    }

    [TestCase("Bien reçu, on reste dehors.", "Copy, on reste dehors.")]
    [TestCase("Noté pour les pneus.", "Copy, noté pour les pneus.")]
    [TestCase("Copy, compris.", "Copy, compris.")]
    public void RadioStyle_SaysCopy(string text, string expected)
    {
        Assert.That(RadioStyle.WithCopy(text), Is.EqualTo(expected));
    }

    [Test]
    public void TrackLimits_AreShortAndWarnBeforePenalty()
    {
        var lines = DrivingRadio.Lines(new[]
        {
            new DrivingEvent { Type = "limites", Corner = "Virage 5", Detail = "large", Points = 2, PointsPerPenalty = 3 },
            new DrivingEvent { Type = "tour-annulé" },
        }, _snap);
        Assert.That(lines, Has.Count.EqualTo(1));
        Assert.That(lines[0].Text, Is.EqualTo("Track limits virage 5 : trop large. Temps annulé. 2 sur 3 : la prochaine, c'est la pénalité."));
    }

    [Test]
    public void DrivingWatch_SeesTrackLimitsAndWearSpike()
    {
        var watch = new DrivingWatch();
        var corners = new List<TrackSegment> { new() { Kind = "corner", Name = "Virage 3", Start = 900, Apex = 1000, End = 1100 } };
        DriverInfo player = Player(0, 0);
        for (int i = 0; i <= 40; i++)
        {
            double t = i * 0.1;
            player = Player(wearRr: i < 20 ? 0.10 : 0.10 + ((i - 20) * 0.0015), dist: 950 + (i * 4));
            player.CarInfo.Controls.TrackLimitsPoints = i < 30 ? 1 : 2;
            watch.Update(t, player, new InputInfo { ThrottlePedalPosition = 0.9, SteeringInput = 0.2 }, 4000, corners, "Race", "Green");
        }

        var events = watch.Drain();
        Assert.That(events.Select(e => e.Type), Does.Contain("limites"));
        DrivingEvent wear = events.FirstOrDefault(e => e.Type == "usure");
        Assert.That(wear, Is.Not.Null, "usure brutale non vue");
        Assert.That(wear.Spike.Wheel, Is.EqualTo("RR"));
        Assert.That(wear.Spike.Phase, Is.EqualTo("sortie"));
        Assert.That(wear.Spike.Corner, Is.EqualTo("Virage 3"));
    }

    [Test]
    public void DrivingWatch_RemindsEngineAtStart()
    {
        var watch = new DrivingWatch();
        for (int i = 0; i < 60; i++)
        {
            DriverInfo p = Player(0.0, 10);
            p.Speed = Velocity.FromKph(0);
            p.CarInfo.EngineRpm = 0;
            p.CarInfo.Controls.Ignition = 0;
            watch.Update(i * 0.1, p, new InputInfo(), 4000, null, "Practice", "Green");
        }

        Assert.That(watch.Drain().Single().Detail, Is.EqualTo("moteur"));
    }

    [Test]
    public void Speech_PausesBetweenWheels()
    {
        Assert.That(FrenchSpeech.ToSpeech("Avant gauche 80 °C, avant droit 90."), Is.EqualTo("Avant gauche quatre-vingts degrés. avant droit quatre-vingt-dix."));
    }

    private static DriverInfo Player(double wearRr, double dist)
    {
        var d = new DriverInfo
        {
            IsPlayer = true,
            DriverSessionId = "1",
            CompletedLaps = 3,
            LapDistance = dist,
            Speed = Velocity.FromKph(150),
            CarInfo = new SecondMonitor.DataModel.Snapshot.Systems.CarInfo(),
        };
        d.CarInfo.EngineRpm = 7000;
        d.CarInfo.CurrentGear = "4";
        d.CarInfo.Controls.Available = true;
        d.CarInfo.Controls.TrackLimitsPointsPerPenalty = 3;
        var w = d.CarInfo.WheelsInfo;
        w.FrontLeft.TyreWear.ActualWear = 0.08;
        w.FrontRight.TyreWear.ActualWear = 0.08;
        w.RearLeft.TyreWear.ActualWear = 0.09;
        w.RearRight.TyreWear.ActualWear = wearRr;
        return d;
    }
}

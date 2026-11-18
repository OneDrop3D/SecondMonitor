namespace SecondMonitor.Hub.Tests;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Strategy;

/// <summary>
/// Retour du 18/11 (LMU) : « une stratégie à deux tours de plus », réglée à la voix. Consommation à viser par tour, menu des stands
/// sur le nouveau plan, où et de combien lever le pied, puis la cible dite à chaque tour jusqu'à l'arrêt.
/// </summary>
[TestFixture]
public class RelaisAllongeTests
{
    /// <summary>Hypercar au Mans : 16 tours bouclés (30 % du 17e), 18,5 à faire, énergie 60 % (7,9 %/tour), essence 62 L (7,33 L/tour). Au plus tard : tour 23.</summary>
    private static RaceSnapshot Snap(double energy = 60, double fuel = 62, int laps = 16, double fraction = 0.3, double lapsLeft = 18.5) => new()
    {
        HasData = true,
        SessionType = "Race",
        Source = "LM Ultimate",
        Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = laps, LapFraction = fraction, Number = 35, ClassId = "h" } },
        Player = new PlayerStrategy
        {
            LapsRemaining = lapsLeft,
            Fuel = new FuelStrategy { Remaining = fuel, Capacity = 100, PerLap = 7.33, LapsLeft = fuel / 7.33, ToAdd = ((lapsLeft + 1) * 7.33) - fuel },
            Energy = new EnergyStrategy { RemainingPct = energy, PerLapPct = 7.9, LapsLeft = energy / 7.9, ToAddPct = ((lapsLeft + 1) * 7.9) - energy },
            PitPlan = new PitPlanDto { PitLoss = 84, PitLossSource = "d'après le circuit" },
        },
    };

    /// <summary>Tour de 3 km : deux lignes droites à pleins gaz (250 km/h) qui finissent en gros freinage, vers les virages 1 et 2.</summary>
    private static (LapDetailDto Lap, IReadOnlyList<TrackSegment> Segments) Track()
    {
        var lap = new LapDetailDto { Lap = 15, Complete = true };
        for (int i = 0; i < 300; i++)
        {
            int k = i % 150; // 0-99 : pleins gaz ; 100-114 : freinage ; 115-149 : virage et relance
            double speed = k < 100 ? 250 : k < 115 ? 250 - ((k - 99) * 10) : 100 + ((k - 115) * 4);
            lap.Speed.Add(speed);
            lap.Throttle.Add(k < 100 ? 100 : k < 115 ? 0 : 60);
            lap.Brake.Add(k is >= 100 and < 115 ? 80 : 0);
        }

        var segments = new List<TrackSegment>
        {
            new() { Kind = "corner", Name = "Virage 1", Apex = 1160 },
            new() { Kind = "corner", Name = "Virage 2", Apex = 2660 },
        };
        return (lap, segments);
    }

    [Test]
    public void ArretTropTardDonneLaCibleParTour()
    {
        RaceSnapshot snap = Snap();
        RacePlanDto plan = RacePlanner.Build(snap);
        Assert.That(plan.WindowLatest, Is.EqualTo(23));
        StopShiftDto s = RacePlanner.Shift(plan, RacePlanner.Resources(snap.Player), 16, 9, 0.3, 18.5, 1, plan.FullStint, true);
        Assert.That(s.Feasible, Is.False);
        Assert.That(s.EnergyPerLapTarget, Is.EqualTo(6.9).Within(0.01), "60 % sur 8,7 tours");
        Assert.That(s.FuelPerLapTarget, Is.EqualTo(7.13).Within(0.01), "62 L sur 8,7 tours");
        Assert.That(s.SavingPct, Is.EqualTo(12.7).Within(0.1));
        Assert.That(s.EnergyPct, Is.EqualTo(85.3).Within(0.1), "on arrive à vide : 10,8 tours d'énergie à remettre");
    }

    [Test]
    public void QuestionDeuxToursDePlusSansRienDecider()
    {
        var memory = new RaceMemory();
        string a = StrategyRadio.Answer("Et si on fait deux tours de plus ?", Snap(), memory, null, Track);
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Oui, tour 25 jouable : cible 6,9 % d'énergie et 7,13 L par tour, 13 % de moins."));
        Assert.That(a, Does.Contain("avant le virage 1 et le virage 2 (estimation sur ton tour 15"));
        Assert.That(a, Does.EndWith("On le fait ?"));
        Assert.That(memory.Pending()?.ProposedLap, Is.EqualTo(25));
        Assert.That(memory.PlannedStopLap, Is.Null);
        Assert.That(memory.PitPlan, Is.Null);
        Assert.That(memory.LiftCoast.TargetPerLap, Is.Null);
    }

    [TestCase("On rallonge le relais de deux tours.")]
    [TestCase("Je veux faire deux tours de plus.")]
    [TestCase("Stratégie à deux tours de plus.")]
    public void DecisionDeuxToursDePlus(string phrase)
    {
        var memory = new RaceMemory();
        string a = StrategyRadio.Answer(phrase, Snap(), memory, null, Track);
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Copy, box au tour 25 : cible 6,9 % d'énergie"));
        Assert.That(a, Does.Contain("On mettra 86 % d'énergie et 80 L"));
        Assert.That(a, Does.EndWith("Je te donne la conso à chaque tour."));
        Assert.That(memory.PlannedStopLap, Is.EqualTo(25));
        Assert.That(memory.LiftCoast.TargetPerLap, Is.EqualTo(6.9).Within(0.01));
        Assert.That(memory.LiftCoast.TargetUnit, Is.EqualTo("%"));
        Assert.That(memory.LiftCoast.UntilLap, Is.EqualTo(25));
        Assert.That(memory.PitPlan.Fuel, Is.EqualTo("plan"), "le menu des stands suit le nouveau plan");
        Assert.That(memory.PitPlan.FuelRatio, Is.EqualTo(0.94).Within(0.001));

        // Le plan de l'écran (et du menu des stands) garde l'arrêt du tour 25, au-delà de la fenêtre : niveaux après l'arrêt.
        RacePlanDto plan = RacePlanner.Build(Snap(), LiftCoast.Context(memory));
        Assert.That(plan.NextStopLap, Is.EqualTo(25));
        Assert.That(plan.PlannedByDriver, Is.True);
        Assert.That(plan.WindowLatest, Is.EqualTo(23), "la fenêtre reste celle de la consommation normale");
        Assert.That(plan.Stops[0].EnergyAfterPct, Is.EqualTo(85.3).Within(0.1));
    }

    [Test]
    public void ToursDePlusPasJouables()
    {
        var memory = new RaceMemory();
        string a = StrategyRadio.Answer("Je veux faire quatre tours de plus.", Snap(), memory, null, Track);
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Tour 27, 4 tours de plus : cible"));
        Assert.That(a, Does.EndWith("pas jouable. Au plus tard : tour 23."));
        Assert.That(memory.PlannedStopLap, Is.Null);
    }

    [Test]
    public void ToursDeMoinsEtUnTourDePlusDansLaFenetre()
    {
        var memory = new RaceMemory();
        string a = StrategyRadio.Answer("On raccourcit le relais de deux tours.", Snap(), memory);
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Copy, box au tour 21"));
        Assert.That(memory.PitPlan.Fuel, Is.EqualTo("plan"));

        // Deux arrêts, fenêtre du tour 22 au 23, arrêt décidé au 22 : un tour de plus reste dans la fenêtre, sans économie.
        string b = StrategyRadio.Answer("Je veux faire un tour de plus.", Snap(lapsLeft: 30), new RaceMemory { PlannedStopLap = 22 });
        TestContext.Out.WriteLine(b);
        Assert.That(b, Does.StartWith("Copy, box au tour 23, toujours 2 arrêts."));
    }

    [Test]
    public void DemandeDeCarburantPasPrisepourLeRelais()
    {
        Assert.That(StrategyRadio.Answer("En rajoutant deux tours de carburant.", Snap(), new RaceMemory()), Is.Null);
        Assert.That(PitOrders.Parse("En rajoutant deux tours de carburant.").FuelLaps, Is.EqualTo(2));
        Assert.That(StrategyRadio.Answer("Il me reste deux tours de plus que lui.", Snap(), new RaceMemory()), Is.Null);
    }

    [Test]
    public void OuLeverLePiedAvecUnEssaiMesure()
    {
        var (lap, segments) = Track();
        var measure = new LiftCoastMeasure { EnergySaved = 0.6, TimeLoss = 0.9 };
        string a = LiftCoast.Where(lap, segments, 1.0, 7.9, "%", measure);
        Assert.That(a, Is.EqualTo("Lève le pied 210 m avant le virage 1 et le virage 2 (ton essai : 0,6 % pour 100 à 150 m)."));

        var memory = new RaceMemory();
        memory.LiftCoast.Measure = measure;
        string decided = StrategyRadio.Answer("On rallonge le relais de deux tours.", Snap(), memory, null, Track);
        TestContext.Out.WriteLine(decided);
        Assert.That(decided, Does.Contain("Environ 1,5 s perdue par tour."));

        // Sans tour gardé ni découpage : le geste de base, dit comme tel.
        Assert.That(LiftCoast.Where(null, null, 1.0, 7.9, "%", null), Is.EqualTo("Lève le pied 100 à 150 m avant les gros freinages (pour commencer, je corrige à chaque tour)."));
        Assert.That(LiftCoast.BrakingZones(lap, segments).Select(z => z.Distance), Is.EqualTo(new[] { 1000.0, 2500.0 }));

        // Circuit pas encore découpé : les freinages par leur distance.
        Assert.That(LiftCoast.Where(lap, new List<TrackSegment>(), 1.0, 7.9, "%", measure), Is.EqualTo("Lève le pied 210 m avant les freinages à 1,0 et 2,5 km (ton essai : 0,6 % pour 100 à 150 m)."));

        // Circuit connu : le nom du lieu.
        segments[0].Place = "Mulsanne";
        segments[1].Place = "1re chicane";
        Assert.That(LiftCoast.BrakingZones(lap, segments).Select(z => z.Name), Is.EqualTo(new[] { "Mulsanne", "la 1re chicane" }));
    }

    [Test]
    public void CibleDiteAChaqueTourJusquALArret()
    {
        var memory = new RaceMemory();
        StrategyRadio.Answer("On rallonge le relais de deux tours.", Snap(), memory, null, Track);
        var laps = new List<LapDto> { new() { Lap = 16, Time = 212, Energy = 7.9, Fuel = 7.33, Valid = true } };

        // Tour 17 : 6,8 % pour une cible de 6,9 %.
        laps.Add(new LapDto { Lap = 17, Time = 213, Energy = 6.8, Fuel = 6.4, Valid = true });
        RadioLine ok = LiftCoast.Check(memory, Snap(energy: 55.2, fuel: 57.5, laps: 17, fraction: 0.02), laps);
        Assert.That(ok.Text, Is.EqualTo("Tour 17 : 6,8 %, dans la cible."));

        // Tour 18 : 7,3 % : il manque 0,4 %, nouvelle cible sur ce qui reste.
        laps.Add(new LapDto { Lap = 18, Time = 212.5, Energy = 7.3, Fuel = 6.8, Valid = true });
        RadioLine over = LiftCoast.Check(memory, Snap(energy: 47.9, fuel: 50.7, laps: 18, fraction: 0.02), laps);
        TestContext.Out.WriteLine(over.Text);
        Assert.That(over.Text, Does.StartWith("Tour 18 : 7,3 %, il manque 0,4 %. Lève le pied plus tôt. Cible 6,9 % jusqu'au tour 25."));
        Assert.That(LiftCoast.Check(memory, Snap(energy: 47.9, fuel: 50.7, laps: 18, fraction: 0.2), laps), Is.Null, "une fois par tour");

        // L'arrêt du tour 25 passé : plus de cible.
        laps.Add(new LapDto { Lap = 25, Time = 240, Energy = 6.9, Fuel = 6.5, Valid = true, Pit = true });
        Assert.That(LiftCoast.Check(memory, Snap(energy: 85, fuel: 80, laps: 25, fraction: 0.02), laps), Is.Null);
        Assert.That(memory.LiftCoast.TargetPerLap, Is.Null);
        Assert.That(memory.LiftCoast.UntilLap, Is.Null);
    }

    [TestCase("Oui")]
    [TestCase("On le fait.")]
    [TestCase("Vas-y")]
    [TestCase("OK")]
    public void OuiAOnLeFaitDecideLePlan(string reply)
    {
        var memory = new RaceMemory();
        StrategyRadio.Answer("Et si on fait deux tours de plus ?", Snap(), memory, null, Track);
        string a = StrategyRadio.Answer(reply, Snap(), memory, null, Track);
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Copy, box au tour 25 : cible 6,9 % d'énergie"));
        Assert.That(memory.PlannedStopLap, Is.EqualTo(25));
        Assert.That(memory.LiftCoast.UntilLap, Is.EqualTo(25), "cible suivie à chaque tour");
        Assert.That(memory.PitPlan.Fuel, Is.EqualTo("plan"), "menu des stands sur le nouveau plan");
        Assert.That(memory.Pending(), Is.Null);
        Assert.That(memory.Entries.Any(n => n.Text.Contains("relais allongé de 2 tours")), Is.True);
    }

    [Test]
    public void NonOuTropTardNeDecideRien()
    {
        var memory = new RaceMemory();
        StrategyRadio.Answer("Et si on fait deux tours de plus ?", Snap(), memory, null, Track);
        Assert.That(StrategyRadio.Answer("Non", Snap(), memory, null, Track), Is.EqualTo("Copy, on garde le plan."));
        Assert.That(memory.PlannedStopLap, Is.Null);
        Assert.That(memory.PitPlan, Is.Null);
        Assert.That(RadioCommands.YesNo("on le fait pas"), Is.Not.True);

        // Plus de 2 minutes après : un « oui » ne décide plus rien ici.
        StrategyRadio.Answer("Et si on fait deux tours de plus ?", Snap(), memory, null, Track);
        memory.Pending().AskedAtUtc = System.DateTime.UtcNow.AddMinutes(-3);
        Assert.That(StrategyRadio.Answer("Oui", Snap(), memory, null, Track), Is.Null);
        Assert.That(memory.PlannedStopLap, Is.Null);
    }

    [Test]
    public void OuiApresDeuxToursDeMoins()
    {
        var memory = new RaceMemory();
        string q = StrategyRadio.Answer("Et si on fait deux tours de moins ?", Snap(), memory);
        Assert.That(q, Does.StartWith("Oui, tour 21 ça passe"));
        Assert.That(q, Does.EndWith("On le fait ?"));
        string a = StrategyRadio.Answer("ok", Snap(), memory);
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Copy, box au tour 21."));
        Assert.That(memory.PlannedStopLap, Is.EqualTo(21));
        Assert.That(memory.PitPlan.Fuel, Is.EqualTo("plan"));
    }

    [Test]
    public void NouvelArretAnnuleLaCible()
    {
        var memory = new RaceMemory();
        StrategyRadio.Answer("On rallonge le relais de deux tours.", Snap(), memory, null, Track);
        StrategyRadio.Answer("Box au tour 22.", Snap(), memory);
        Assert.That(memory.PlannedStopLap, Is.EqualTo(22));
        Assert.That(memory.LiftCoast.UntilLap, Is.Null);
        Assert.That(memory.LiftCoast.TargetPerLap, Is.Null);
    }
}

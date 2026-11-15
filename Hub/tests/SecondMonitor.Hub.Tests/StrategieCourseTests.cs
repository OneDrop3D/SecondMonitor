namespace SecondMonitor.Hub.Tests;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Strategy;

/// <summary>
/// Retours après la course de 2 h au Mans (15/11) : nombre d'arrêts, arrêt un tour plus tôt / plus tard, « au tour 21 »,
/// ratio carburant / énergie, options, tours restants, lift and coast (essai, référence, « pas d'erreur », verdict),
/// batterie comprise, écart à chaque passage de la ligne, « box » sans 108 % d'énergie.
/// </summary>
[TestFixture]
public class StrategieCourseTests
{
    /// <summary>Hypercar au Mans : 16 tours bouclés, 18,5 tours à faire, énergie 45 % (7,9 %/tour), essence 50 L (7,33 L/tour, réservoir 100 L).</summary>
    private static RaceSnapshot Snap(double lapsLeft = 18.5, double energy = 45, double fuel = 50, double lapFraction = 0.3)
    {
        var snap = new RaceSnapshot
        {
            HasData = true,
            SessionType = "Race",
            Source = "LM Ultimate",
            Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = 16, LapFraction = lapFraction, Number = 35, ClassId = "h" } },
            Player = new PlayerStrategy
            {
                LapsRemaining = lapsLeft,
                Fuel = new FuelStrategy { Remaining = fuel, Capacity = 100, PerLap = 7.33, LapsLeft = fuel / 7.33, ToAdd = ((lapsLeft + 1) * 7.33) - fuel },
                Energy = new EnergyStrategy { RemainingPct = energy, PerLapPct = 7.9, LapsLeft = energy / 7.9, ToAddPct = ((lapsLeft + 1) * 7.9) - energy },
                PitPlan = new PitPlanDto { PitLoss = 84, PitLossSource = "d'après le circuit" },
            },
        };
        return snap;
    }

    private static List<LapDto> Laps(params (int Lap, double Time, double Energy, double Fuel)[] laps) =>
        laps.Select(l => new LapDto { Lap = l.Lap, Time = l.Time, Energy = l.Energy, Fuel = l.Fuel, Valid = true }).ToList();

    [Test]
    public void PlanCompteLesArretsEtLaFenetre()
    {
        RacePlanDto plan = RacePlanner.Build(Snap());
        Assert.That(plan.StopsNeeded, Is.EqualTo(2));
        Assert.That(plan.Limiter, Is.EqualTo("énergie virtuelle"));
        Assert.That(plan.WindowLatest, Is.EqualTo(21), "45 % à 7,9 %/tour : 5,7 tours, à 30 % du tour 17");
        Assert.That(plan.WindowEarliest, Is.LessThanOrEqualTo(plan.WindowLatest));
        Assert.That(plan.Stops, Has.Count.EqualTo(2));
        Assert.That(plan.Stops.All(s => s.EnergyPct <= 100), "jamais plus de 100 % d'énergie à un arrêt");
        Assert.That(plan.Stops[^1].Last, Is.True);
        Assert.That(plan.Stops[^1].EnergyPct, Is.LessThan(plan.Stops[0].EnergyPct), "le dernier arrêt ne remet que ce qu'il faut pour finir");
        Assert.That(plan.IdealRatio, Is.EqualTo(0.928).Within(0.001));
        Assert.That(plan.OneLess.Stops, Is.EqualTo(1));
        Assert.That(plan.OneLess.Realistic, Is.True);
        Assert.That(plan.OneLess.EnergySavingPerLap, Is.EqualTo(0.46).Within(0.02));
    }

    [Test]
    public void PlanSansArretQuandOnPeutFinir()
    {
        RacePlanDto plan = RacePlanner.Build(Snap(lapsLeft: 4));
        Assert.That(plan.StopsNeeded, Is.EqualTo(0));
        Assert.That(plan.CanFinish, Is.True);
        Assert.That(StrategyRadio.Answer("On peut finir la course avec ce plein ?", Snap(lapsLeft: 4), new RaceMemory()), Does.StartWith("Oui"));
    }

    [TestCase("Il faudra faire encore deux arrêts.", "Oui. Encore 2 arrêts")]
    [TestCase("On doit faire encore trois box.", "Non. Encore 2 arrêts")]
    [TestCase("Qu'est-ce qu'on peut faire un box ou deux boxs?", "Encore 2 arrêts")]
    [TestCase("Combien d'arrêts il reste ?", "Encore 2 arrêts")]
    public void NombreDArrets(string question, string expected)
    {
        string a = StrategyRadio.Answer(question, Snap(), new RaceMemory());
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith(expected));
        Assert.That(a, Does.Contain("Prochain au tour"));
        Assert.That(a, Does.Contain("Pour un arrêt"), "ce qu'il faut économiser pour un arrêt de moins");
    }

    [Test]
    public void QuatriemeArret()
    {
        RaceSnapshot snap = Snap();
        snap.Drivers[0].PitStops = 2;
        string a = StrategyRadio.Answer("Je suis obligé de faire un quatrième arrêt, non?", snap, new RaceMemory());
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.StartWith("Oui."));
        Assert.That(a, Does.Contain("4 arrêts en tout"));
    }

    [Test]
    public void ArretDecaleDUnTourEtAuTourVingtEtUn()
    {
        var memory = new RaceMemory();
        string later = StrategyRadio.Answer("Non, autour vingt et un.", Snap(), memory);
        TestContext.Out.WriteLine(later);
        Assert.That(later, Does.StartWith("Copy, box au tour 21"));
        Assert.That(memory.PlannedStopLap, Is.EqualTo(21));

        string tooLate = StrategyRadio.Answer("Je boxe au tour 23.", Snap(), new RaceMemory());
        TestContext.Out.WriteLine(tooLate);
        Assert.That(tooLate, Does.Contain("trop tard"));
        Assert.That(tooLate, Does.Contain("Au plus tard : tour 21"));

        var m2 = new RaceMemory { PlannedStopLap = 20 };
        string next = StrategyRadio.Answer("Je boxe au tour d'après.", Snap(), m2);
        Assert.That(next, Does.StartWith("Copy, box au tour 21"));

        string question = StrategyRadio.Answer("Et si je boxe un tour plus tôt ?", Snap(), new RaceMemory { PlannedStopLap = 21 });
        TestContext.Out.WriteLine(question);
        Assert.That(question, Does.StartWith("Oui, tour 20"));
    }

    [Test]
    public void PhrasesQuiNeSontPasDesArrets()
    {
        Assert.That(StrategyRadio.Answer("Le tour 5 était bon.", Snap(), new RaceMemory()), Is.Null);
        Assert.That(StrategyRadio.Answer("Si je boxe maintenant, je ressors où?", Snap(), new RaceMemory()), Is.Null, "c'est la position à la sortie");
        Assert.That(StrategyRadio.Answer("Box box.", Snap(), new RaceMemory()), Is.Null, "demande d'arrêt, traitée par PitOrders");
        Assert.That(StrategyRadio.Answer("Dernier micro secteur, j'ai perdu combien de temps par rapport à mon dernier tour.", Snap(), new RaceMemory()), Is.Null);
    }

    [Test]
    public void RatioCarburantEnergie()
    {
        string a = StrategyRadio.Answer("Calcul le ratio carburant énergie. Je suis à zéro virgule quatre-vingt-dix-sept pour le moment.", Snap(), new RaceMemory());
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.Contain("À 0,97"));
        Assert.That(a, Does.Contain("en trop"));
        Assert.That(a, Does.Contain("0,94"));
        Assert.That(StrategyRadio.Answer("le ratio", Snap(), new RaceMemory()), Does.StartWith("Ratio idéal 0,93"));
    }

    [Test]
    public void ToursRestantsEtOptions()
    {
        Assert.That(StrategyRadio.Answer("Il y a combien de tours avant la fin de la course ?", Snap(), new RaceMemory()), Does.StartWith("Encore 19 tours"));
        string options = StrategyRadio.Answer("Qu'est-ce qu'on peut jouer ?", Snap(), new RaceMemory());
        TestContext.Out.WriteLine(options);
        Assert.That(options, Does.StartWith("Plan : 2 arrêts."));
    }

    [Test]
    public void Definitions()
    {
        Assert.That(StrategyRadio.Answer("C'est quoi l'énergie virtuelle?", Snap(), new RaceMemory()), Does.StartWith("L'énergie virtuelle, c'est le quota"));
        Assert.That(StrategyRadio.Answer("c'est quoi le lift and coast", Snap(), new RaceMemory()), Does.StartWith("Lift and coast :"));
    }

    [Test]
    public void LiftAndCoastDeBoutEnBout()
    {
        var memory = new RaceMemory();
        var laps = Laps((14, 212.5, 7.9, 7.33), (15, 212.1, 7.9, 7.33), (16, 211.8, 7.9, 7.33));
        RaceSnapshot snap = Snap();

        // Le pilote désigne sa référence, puis lance l'essai (dictée « Lyssenko »).
        Assert.That(LiftCoast.Handle("J'ai pas fait d'erreur, le tour 16 c'est ma référence", memory, snap, laps), Does.StartWith("Copy, tour 16 en référence"));
        string armed = LiftCoast.Handle("Je fais un tour en Lyssenko", memory, snap, laps);
        TestContext.Out.WriteLine(armed);
        Assert.That(armed, Does.Contain("tour 18"));
        Assert.That(memory.LiftCoast.State, Is.EqualTo("armé"));

        // Tour 18 bouclé : 0,9 s perdue, 0,6 % d'énergie et 0,45 L économisés.
        laps.Add(new LapDto { Lap = 17, Time = 212.0, Energy = 7.9, Fuel = 7.33, Valid = true });
        laps.Add(new LapDto { Lap = 18, Time = 212.7, Energy = 7.3, Fuel = 6.88, Valid = true });
        snap.Drivers[0].Laps = 18;
        snap.Drivers[0].LapFraction = 0.02;
        RadioLine check = LiftCoast.Check(memory, snap, laps);
        TestContext.Out.WriteLine(check.Text);
        Assert.That(check.IsQuestion, Is.True);
        Assert.That(check.Text, Does.Contain("0,90 s de plus que le tour 16"));
        Assert.That(check.Text, Does.EndWith("Tour propre, sans erreur ?"));

        // « Non, pas d'erreur » : mesure validée, verdict chiffré.
        string verdict = LiftCoast.Handle("Non, j'ai pas fait d'erreur.", memory, Snap(), laps);
        TestContext.Out.WriteLine(verdict);
        Assert.That(memory.LiftCoast.Measure, Is.Not.Null);
        Assert.That(verdict, Does.Contain("Un arrêt au lieu de 2 arrêts"));
        Assert.That(verdict, Does.Contain("On gagne"));
        Assert.That(verdict, Does.EndWith("On le fait ?"));

        string go = LiftCoast.Handle("Oui", memory, Snap(), laps);
        TestContext.Out.WriteLine(go);
        Assert.That(go, Does.StartWith("Copy, lift and coast jusqu'à la fin"));
        Assert.That(memory.LiftCoast.TargetPerLap, Is.EqualTo(7.44).Within(0.05));

        // Le plan de l'écran tient compte de la mesure.
        RacePlanDto plan = RacePlanner.Build(Snap(), LiftCoast.Context(memory));
        Assert.That(plan.OneLess.NetGain, Is.GreaterThan(50));
    }

    [Test]
    public void LiftAndCoastAvecErreurOnRefait()
    {
        var memory = new RaceMemory();
        var laps = Laps((15, 212.1, 7.9, 7.33), (16, 211.8, 7.9, 7.33));
        RaceSnapshot snap = Snap(lapFraction: 0.5);
        LiftCoast.Handle("on teste le lift and coast", memory, snap, laps);
        laps.Add(new LapDto { Lap = 17, Time = 213, Energy = 7.9, Fuel = 7.3, Valid = true });
        laps.Add(new LapDto { Lap = 18, Time = 215.0, Energy = 7.2, Fuel = 6.9, Valid = true });
        snap.Drivers[0].Laps = 18;
        Assert.That(LiftCoast.Check(memory, snap, laps), Is.Not.Null);
        string retry = LiftCoast.Handle("J'ai fait une erreur au virage 6", memory, snap, laps);
        Assert.That(retry, Does.Contain("On refait"));
        Assert.That(memory.LiftCoast.State, Is.EqualTo("armé"));
        Assert.That(memory.LiftCoast.Measure, Is.Null);
    }

    [Test]
    public void LiftAndCoastQuestionDeValeurSansMesure()
    {
        var memory = new RaceMemory();
        string a = LiftCoast.Handle("Est-ce que si je fais du lift and coast on peut gagner du temps ?", memory, Snap(), Laps((16, 211.8, 7.9, 7.33)));
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.Contain("il faut économiser 0,5 % d'énergie par tour"));
        Assert.That(a, Does.EndWith("Je lance l'essai au prochain tour ?"));
        Assert.That(LiftCoast.Handle("oui", memory, Snap(), Laps((16, 211.8, 7.9, 7.33))), Does.StartWith("Copy, lift and coast"));
    }

    [Test]
    public void BoxSansDepasserCentPourCent()
    {
        RaceSnapshot snap = Snap(lapsLeft: 13, energy: 8, fuel: 11);
        string reply = PitOrders.Confirm(PitOrders.Parse("Box box."), new RaceMemory(), snap, 20);
        TestContext.Out.WriteLine(reply);
        Assert.That(reply, Does.Contain("92 % d'énergie"));
        Assert.That(reply, Does.Not.Contain("108"));
    }

    [Test]
    public void BatterieDAbord()
    {
        Assert.That(RaceBriefing.DetectTopics("consommation de la batterie").First(), Is.EqualTo(RaceBriefing.Topic.Hybrid));
        Assert.That(RaceBriefing.DetectTopics("Matière d'énergie électrique sur la batterie.").First(), Is.EqualTo(RaceBriefing.Topic.Hybrid));
        Assert.That(RaceBriefing.DetectTopics("Matière d'énergie électrique sur la batterie."), Does.Not.Contain(RaceBriefing.Topic.Energy));
        Assert.That(RaceBriefing.DetectTopics("je resort en quelle positionn"), Is.EqualTo(new[] { RaceBriefing.Topic.Strategy }));
        Assert.That(RaceBriefing.DetectTopics("Est-ce que j'ai plus de batterie au même endroit que le tour précédent ?"), Is.EqualTo(new[] { RaceBriefing.Topic.Hybrid }));
    }

    [Test]
    public void EcartAChaquePassageDeLaLigne()
    {
        var parsed = GapWatcher.Parse("Chaque passage de la ligne d'arrivée, donne-moi l'écart entre moi et le pilote derrière moi.");
        Assert.That(parsed, Is.Not.Null);
        GapWatch w = parsed.Value.Watch;
        Assert.That(w.PerLap, Is.True);
        Assert.That(w.Side, Is.EqualTo("behind"));

        RaceSnapshot snap = Snap(lapFraction: 0.05);
        snap.Player.Battles = new List<BattleDto> { new() { Id = "50", Number = 50, Side = "behind", Gap = 27.2 } };
        Assert.That(GapWatcher.Check(w, snap, System.DateTime.UtcNow), Is.EqualTo("Numéro 50 à 27,2 s."));
        Assert.That(GapWatcher.Check(w, snap, System.DateTime.UtcNow.AddSeconds(30)), Is.Null, "une seule annonce par tour");
        snap.Player.Battles[0].Gap = 28.3;
        snap.Drivers[0].Laps = 17;
        Assert.That(GapWatcher.Check(w, snap, System.DateTime.UtcNow.AddSeconds(220)), Is.EqualTo("Numéro 50 à 28,3 s, tu prends 1,1 s."));
    }

    [TestCase("vingt et un", "21")]
    [TestCase("zero virgule quatre vingt dix sept", "0 virgule 97")]
    [TestCase("un arret de moins", "un arret de moins")]
    [TestCase("autour vingt et un", "au tour 21")]
    [TestCase("soixante et onze", "71")]
    public void NombresDictes(string text, string expected)
    {
        Assert.That(StrategyRadio.Numbers(text), Is.EqualTo(expected));
    }
}

namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Strategy;
using SecondMonitor.Hub.Core.Track;

/// <summary>
/// Temps perdu au stand connu avant le premier arrêt (demande du pilote, 14/11) : voie des stands de chaque circuit
/// (Track/Library/pitlane.json) + arrêt habituel de la catégorie, remplacés par la mesure dès qu'un arrêt est vu.
/// </summary>
[TestFixture]
public class VoieDesStandsTests
{
    [Test]
    public void FichierLu_ChaqueCircuitAUneValeurOuUneRaison()
    {
        Assert.That(PitLaneLibrary.Circuits, Has.Count.GreaterThanOrEqualTo(TrackLibrary.All.Count), "une ligne par circuit de la bibliothèque");
        var ids = TrackLibrary.All.Select(t => t.Id).ToHashSet();
        Assert.That(PitLaneLibrary.Circuits.Where(c => !ids.Contains(c.Id)).Select(c => c.Id), Is.Empty, "pas de circuit inconnu de la bibliothèque des tracés");
        Assert.That(PitLaneLibrary.Circuits.GroupBy(c => c.Id).Where(g => g.Count() > 1).Select(g => g.Key), Is.Empty, "un circuit n'apparaît qu'une fois");

        foreach (LibraryTrack t in TrackLibrary.All)
        {
            PitLaneEntry e = PitLaneLibrary.Get(t.Id);
            Assert.That(e, Is.Not.Null, t.Id);
            if (e.PitLaneLossSeconds is not double loss)
            {
                Assert.That(e.Reason, Is.Not.Null.And.Not.Empty, $"{t.Id} : pas de valeur, il faut dire pourquoi");
                continue;
            }

            foreach (PitLaneValue v in new[] { (PitLaneValue)e }.Concat(e.Variants).Where(v => v.PitLaneLossSeconds.HasValue))
            {
                Assert.That(v.PitLaneLossSeconds, Is.InRange(5, 90), $"{t.Id} {v.Category} : passage dans la voie, sans l'arrêt");
                Assert.That(v.SpeedLimitKph, Is.InRange(30, 120), $"{t.Id} {v.Category} : vitesse limite");
                Assert.That(v.Confidence, Is.GreaterThan(0).And.LessThanOrEqualTo(1), $"{t.Id} {v.Category}");
                Assert.That(v.Category, Is.Not.Null.And.Not.Empty, $"{t.Id} : catégorie");
                Assert.That(v.Method, Is.Not.Null.And.Not.Empty, $"{t.Id} {v.Category} : méthode");
                Assert.That(v.Sources, Is.Not.Empty, $"{t.Id} {v.Category} : sources");
                Assert.That(v.Sources, Has.All.StartsWith("http"), $"{t.Id} {v.Category} : des adresses");
                if (v.IsEstimate)
                {
                    Assert.That(v.Confidence, Is.LessThan(0.5), $"{t.Id} : une estimation géométrique garde une confiance basse");
                    Assert.That(v.PitLaneLength, Is.GreaterThan(0), $"{t.Id} : l'estimation garde la longueur pour pouvoir recalculer");
                }
            }
        }
    }

    [Test]
    public void TempsALArret_ParJeuEtCategorie()
    {
        Assert.That(PitLaneLibrary.StopTime("jeu inconnu", "voiture inconnue"), Is.Not.Null, "valeur générique « * »");
        Assert.That(PitLaneLibrary.StopTime("LMU", "Hypercar").Seconds, Is.InRange(15, 90), "ravitaillement et pneus en endurance");
        Assert.That(PitLaneLibrary.StopTime("F1", "F1").Seconds, Is.InRange(1.5, 6), "pneus seuls en F1");
        Assert.That(PitLaneLibrary.StopTimes, Has.All.Matches<StopTimeEntry>(s => s.Sources.Count > 0 && !string.IsNullOrEmpty(s.Detail)), "chaque valeur est documentée");
    }

    [Test]
    public void LeMans_Reconnu_DonneSaValeur()
    {
        RaceSnapshot snap = Snap("Circuit de la Sarthe", string.Empty, 13626);
        PitPlanDto plan = PitPlanner.Build(snap, new Dictionary<string, DriverTracker>());

        PitLaneValue lane = PitLaneLibrary.Get("fr-le-mans").For("WEC", 60);
        double stop = PitLaneLibrary.StopTime("LMU", "Hypercar").Seconds;
        Assert.That(lane?.PitLaneLossSeconds, Is.Not.Null, "Le Mans a une valeur WEC");
        Assert.That(plan.PitLossMeasured, Is.False);
        Assert.That(plan.PitLaneLoss, Is.EqualTo(lane.PitLaneLossSeconds).Within(0.05));
        Assert.That(plan.StopTimeEstimate, Is.EqualTo(stop));
        Assert.That(plan.PitLoss, Is.EqualTo(lane.PitLaneLossSeconds.Value + stop).Within(0.11));
        Assert.That(plan.PitLoss, Is.Not.EqualTo(PitPlanner.DefaultPitLoss), "plus les 22 s par défaut");
        Assert.That(plan.PitLossSource, Does.StartWith("d'après le circuit : ").And.Contain("s de voie des stands et environ").And.Contain("s d'arrêt"));
        TestContext.Out.WriteLine($"{plan.PitLoss} s ({plan.PitLossSource})");

        string radio = RaceBriefing.PitLossAnswer("combien de temps je perds au stand ?", WithPlan(snap, plan));
        Assert.That(radio, Does.Contain("secondes de voie des stands").And.Contain("d'arrêt"));
        Assert.That(radio, Does.Contain("dès qu'un arrêt est vu"));
        TestContext.Out.WriteLine(radio);
    }

    [Test]
    public void LaMesure_RemplaceLEstimation()
    {
        RaceSnapshot snap = Snap("Circuit de la Sarthe", string.Empty, 13626);
        var rival = new DriverTracker("rival");
        rival.Laps.AddRange(new[]
        {
            new LapRecord { Lap = 1, Time = 212.0 },
            new LapRecord { Lap = 2, Time = 210.0 },
            new LapRecord { Lap = 3, Time = 210.5 },
            new LapRecord { Lap = 4, Time = 241.0, IsPitLap = true },
            new LapRecord { Lap = 5, Time = 249.0, IsOutLap = true },
        });
        PitPlanDto plan = PitPlanner.Build(snap, new Dictionary<string, DriverTracker> { ["rival"] = rival });

        Assert.That(plan.PitLossMeasured, Is.True);
        Assert.That(plan.PitLoss, Is.EqualTo(241 + 249 - (2 * 210.5)).Within(0.11), "tour d'entrée + tour de sortie − deux tours normaux");
        Assert.That(plan.PitLossSource, Is.EqualTo("mesuré sur 1 arrêt"));
        Assert.That(plan.PitLaneLoss, Is.Null, "plus d'estimation une fois mesuré");
        Assert.That(RaceBriefing.PitLossAnswer("combien je perds au stand", WithPlan(snap, plan)), Does.Contain("mesuré sur 1 arrêt"));
    }

    [Test]
    public void CircuitInconnu_GardeLaValeurParDefaut()
    {
        PitPlanDto plan = PitPlanner.Build(Snap("Circuit de démonstration", "GP", 5200), new Dictionary<string, DriverTracker>());
        Assert.That(plan.PitLoss, Is.EqualTo(PitPlanner.DefaultPitLoss));
        Assert.That(plan.PitLossSource, Is.EqualTo("estimation, aucun arrêt vu pour l'instant"));
    }

    [Test]
    public void LaCategorieDuJeuChoisitLaValeur()
    {
        // Monza : valeur F1 (80 km/h) pour un jeu F1, valeur WEC (60 km/h) pour LMU, forcément plus longue.
        PitLaneEntry monza = PitLaneLibrary.Get("it-1922");
        PitLaneValue f1 = monza.For("F1", null);
        PitLaneValue wec = monza.For("WEC", 60);
        Assert.That(f1?.PitLaneLossSeconds, Is.Not.Null);
        Assert.That(wec?.PitLaneLossSeconds, Is.GreaterThan(f1.PitLaneLossSeconds), "à 60 km/h la voie coûte plus qu'à 80");

        PitPlanDto lmu = PitPlanner.Build(Snap("Autodromo Nazionale Monza", string.Empty, 5793), new Dictionary<string, DriverTracker>());
        PitPlanDto game = PitPlanner.Build(Snap("Monza", string.Empty, 5793, source: "F1 2024", car: "Ferrari SF-24", cls: "F1"), new Dictionary<string, DriverTracker>());
        Assert.That(lmu.PitLaneLoss, Is.EqualTo(wec.PitLaneLossSeconds).Within(0.05));
        Assert.That(game.PitLaneLoss, Is.EqualTo(f1.PitLaneLossSeconds).Within(0.05));
        Assert.That(game.StopTimeEstimate, Is.LessThan(lmu.StopTimeEstimate), "pneus seuls en F1, ravitaillement et pneus en endurance");
    }

    [Test]
    public void RecalculPourUneAutreVitesseLimite()
    {
        var entry = new PitLaneEntry { Id = "essai", Category = "F1", PitLaneLossSeconds = 20, SpeedLimitKph = 80, PitLaneLength = 400, Confidence = 0.8, Method = "publié" };
        PitLaneValue at60 = entry.For("WEC", 60);
        Assert.That(at60.PitLaneLossSeconds, Is.EqualTo(20 + (400 * 3.6 * ((1.0 / 60) - (1.0 / 80)))).Within(0.05), "6 s de plus sur 400 m à 60 au lieu de 80 km/h");
        Assert.That(at60.Method, Is.EqualTo("recalculé"));
        Assert.That(at60.Confidence, Is.LessThan(0.8));
        Assert.That(entry.For("F1", 80).PitLaneLossSeconds, Is.EqualTo(20));
    }

    private static RaceSnapshot Snap(string track, string layout, double length, string source = "LM Ultimate", string car = "Porsche 963", string cls = "Hypercar") => new()
    {
        HasData = true,
        Source = source,
        SessionType = "Race",
        Track = new TrackDto { Name = track, Layout = layout, Length = length },
        Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = 3, Car = car, ClassName = cls, ClassId = cls, ClassPosition = 1 } },
        Player = new PlayerStrategy(),
    };

    private static RaceSnapshot WithPlan(RaceSnapshot snap, PitPlanDto plan)
    {
        snap.Player.PitPlan = plan;
        return snap;
    }
}

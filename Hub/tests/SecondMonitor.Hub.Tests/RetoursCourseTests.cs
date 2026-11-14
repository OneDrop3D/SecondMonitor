namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Strategy;

/// <summary>
/// Retours après la course d'une heure au Mans (14/11, journal radio du pilote) : transcriptions Qwen3-ASR, temps perdu au stand,
/// « box » qui reprend le plan calculé, pneus demandés, écart annoncé, phrases ratées, accusés variés, moins de bavardage.
/// </summary>
[TestFixture]
public class RetoursCourseTests
{
    [TestCase("language French<asr_text>Box box box box.", "Box box box box.")]
    [TestCase("language French<asr_text>Le niveau d'essence, quel est le niveau d'essence?", "Le niveau d'essence, quel est le niveau d'essence?")]
    [TestCase("language None<asr_text>", "")]
    [TestCase("<asr_text>Mets de la musique</asr_text>", "Mets de la musique")]
    [TestCase("Une phrase normale.", "Une phrase normale.")]
    public void TranscriptionQwenNettoyee(string raw, string clean)
    {
        Assert.That(RadioLexicon.CleanTranscript(raw), Is.EqualTo(clean));
    }

    [Test]
    public void CommandeEnDebutDePhraseRetrouvee()
    {
        // « ^(mets|lance)… » : la balise devant empêchait la commande d'être reconnue.
        Assert.That(SecondMonitor.Hub.Core.Music.MusicCommands.Parse(RadioLexicon.CleanTranscript("language French<asr_text>mets de la musique")), Is.Not.Null);
    }

    private static RaceSnapshot Snap(double? fuelToAdd = 3, double? energyToAdd = 10) => new()
    {
        HasData = true,
        SessionType = "Race",
        Source = "LM Ultimate",
        Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = 9 } },
        Player = new PlayerStrategy
        {
            Fuel = new FuelStrategy { ToAdd = fuelToAdd },
            Energy = new EnergyStrategy { ToAddPct = energyToAdd },
            PitPlan = new PitPlanDto { PitLoss = 22, PitLossSource = "estimation, aucun arrêt vu pour l'instant" },
        },
    };

    [Test]
    public void TempsPerduAuStand()
    {
        string a = RaceBriefing.PitLossAnswer("Je suis intéressé, combien de temps prend un arrêt au vingt-quatre heures du Mans?", Snap());
        Assert.That(a, Does.Contain("22 secondes"));
        Assert.That(a, Does.Not.Contain("(estimée ("), "plus de parenthèses imbriquées");
        Assert.That(RaceBriefing.PitLossAnswer("combien je perds au stand", Snap()), Is.Not.Null);
        Assert.That(RaceBriefing.PitLossAnswer("le temps de descendre aux stands", Snap()), Is.Not.Null);
        Assert.That(RaceBriefing.PitLossAnswer("je rentre au stand quand ?", Snap()), Is.Null, "c'est le tour d'arrêt qui est demandé");
        Assert.That(RaceBriefing.PitLossAnswer("le niveau d'essence", Snap()), Is.Null);

        RaceSnapshot after = Snap();
        after.Player.PitPlan.PlayerLastStopLap = 12;
        after.Player.PitPlan.PlayerLastStopLoss = 31.4;
        Assert.That(RaceBriefing.PitLossAnswer("combien de temps m'a coûté l'arrêt", after), Does.Contain("tour 11 : 31 secondes"));
    }

    [Test]
    public void PhraseRepeteeDiteUneFois()
    {
        Assert.That(RadioFormat.WithoutRepeats("Si tu rentres maintenant, tu ressors P1. Si tu rentres maintenant, tu ressors P1. Un arrêt coûte 22 s."),
            Is.EqualTo("Si tu rentres maintenant, tu ressors P1. Un arrêt coûte 22 s."));
    }

    [Test]
    public void BoxReprendLePlanCalcule()
    {
        var memory = new RaceMemory();
        PitOrder order = PitOrders.Parse("Box box box box.");
        string reply = PitOrders.Confirm(order, memory, Snap(), 9);
        Assert.That(reply, Does.Contain("3 L et 10 % d'énergie"));
        Assert.That(memory.PitPlan.Fuel, Is.Not.Null, "le menu des stands recevra le carburant et l'énergie");
        // Rien à ajouter : pas de quantité inventée.
        var empty = new RaceMemory();
        Assert.That(PitOrders.Confirm(PitOrders.Parse("box box"), empty, Snap(0, 0), 9), Does.Not.Contain(" L"));
        Assert.That(empty.PitPlan.Fuel, Is.Null);
    }

    [TestCase("Des pneus parce que les miens ont des plats.")]
    [TestCase("on change les pneus")]
    [TestCase("je veux des pneus neufs")]
    public void PneusDemandes(string phrase)
    {
        Assert.That(PitOrders.Parse(phrase)?.Tyres, Is.Not.Null, phrase);
    }

    [Test]
    public void QuestionSurLesPneusResteUneQuestion()
    {
        Assert.That(PitOrders.Parse("est-ce qu'il faut que je change de pneus?"), Is.Null);
    }

    [TestCase("J'ai réglé. J'ai baissé de un point la brake balance.")]
    [TestCase("Ok, tu peux me parler de la répartition des freins depuis le début ?")]
    [TestCase("la répartition est à combien")]
    public void RepartitionNEstPasUneReparation(string phrase)
    {
        // Course de 15:16 : « répar(tition) » passait pour « réparation » et réglait de nouveau le menu des stands.
        Assert.That(PitOrders.Parse(phrase)?.Repair ?? false, Is.False, phrase);
    }

    [Test]
    public void ReparationToujoursComprise()
    {
        Assert.That(PitOrders.Parse("au prochain arrêt je veux la réparation")?.Repair, Is.True);
        Assert.That(PitOrders.Parse("box box, répare la voiture aussi")?.Repair, Is.True);
    }

    [Test]
    public void ToursDeCarburantEnPlus()
    {
        PitOrder order = PitOrders.Parse("En arrêt, en rajoutant deux tours de carburant");
        Assert.That(order?.Fuel, Is.EqualTo("tours"));
        Assert.That(order.FuelLaps, Is.EqualTo(2));
        var memory = new RaceMemory();
        Assert.That(PitOrders.Confirm(order, memory, Snap(), 9), Does.Contain("arburant pour 2 tours de plus"));
        Assert.That(memory.PitPlan.FuelLaps, Is.EqualTo(2));
    }

    [Test]
    public void EcartAnnonceAChaqueSeconde()
    {
        var cmd = GapWatcher.Parse("Il m'annonce l'écart à chaque fois qu'il baisse d'une seconde.");
        Assert.That(cmd, Is.Not.Null);
        Assert.That(cmd!.Value.Start, Is.True);
        Assert.That(cmd.Value.Watch.Side, Is.EqualTo("ahead"));
        Assert.That(GapWatcher.Parse("arrête les écarts")!.Value.Start, Is.False);
        Assert.That(GapWatcher.Parse("quel est l'écart avec la voiture devant ?"), Is.Null, "une simple question d'écart");
        Assert.That(GapWatcher.Parse("annonce l'écart avec celui de derrière toutes les deux secondes")!.Value.Watch.Side, Is.EqualTo("behind"));

        GapWatch w = cmd.Value.Watch;
        RaceSnapshot snap(double gap) => new()
        {
            HasData = true,
            Player = new PlayerStrategy { Battles = new List<BattleDto> { new() { Id = "83", Number = 83, Side = "ahead", Gap = gap } } },
        };
        var t0 = new DateTime(2026, 11, 14, 17, 0, 0, DateTimeKind.Utc);
        Assert.That(GapWatcher.Check(w, snap(7.0), t0), Is.Null, "première mesure : point de départ");
        Assert.That(GapWatcher.Check(w, snap(6.5), t0.AddSeconds(30)), Is.Null, "moins d'une seconde");
        Assert.That(GapWatcher.Check(w, snap(5.9), t0.AddSeconds(60)), Is.EqualTo("Numéro 83 à 5,9 s, tu reprends."));
        Assert.That(GapWatcher.Check(w, snap(4.8), t0.AddSeconds(63)), Is.Null, "pas deux annonces en 8 s");
        Assert.That(GapWatcher.Check(w, snap(4.7), t0.AddSeconds(75)), Does.Contain("tu reprends"));
    }

    [TestCase("C'est une cause pour.", true)]
    [TestCase("Tests attendus dans les.", true)]
    [TestCase("Station essence.", false)]
    [TestCase("Avec le pilote devant, le premier pilote.", false)]
    [TestCase("L'arrière glisse en sortie.", false)]
    [TestCase("combien de tours ?", false)]
    public void PhraseRateeOnDemandeDeRepeter(string phrase, bool unclear)
    {
        Assert.That(RadioCommands.IsUnclear(phrase), Is.EqualTo(unclear), phrase);
    }

    [Test]
    public void QuestionDifficileOuSimple()
    {
        var simple = new BriefingResult { Topics = new List<RaceBriefing.Topic> { RaceBriefing.Topic.Fuel }, Suggestion = "Il te reste 5 tours." };
        Assert.That(RaceBriefing.IsHardQuestion("combien d'essence ?", simple), Is.False, "réponse calculée : pas besoin du gros modèle");
        var free = new BriefingResult { Topics = new List<RaceBriefing.Topic> { RaceBriefing.Topic.Pace } };
        Assert.That(RaceBriefing.IsHardQuestion("pourquoi je perds du temps dans le premier secteur ?", free), Is.True);
        Assert.That(RaceBriefing.IsHardQuestion("mon rythme ?", free), Is.False);
    }

    [Test]
    public void AccusesVariesPasToujoursCopy()
    {
        var heads = Enumerable.Range(0, 8).Select(_ => RadioStyle.Vary("Copy, box ce tour.", english: true)).ToList();
        Assert.That(heads.Count(h => h.StartsWith("Copy")), Is.LessThan(heads.Count / 2), string.Join(" | ", heads));
        Assert.That(heads, Has.All.Contain("ox ce tour"));
        Assert.That(Enumerable.Range(0, 8).Select(_ => RadioStyle.Vary("Copy. Je règle le menu.", english: false)), Has.None.StartWith("Copy"));
        Assert.That(RadioStyle.Vary("Il te reste 5 tours.", english: true), Is.EqualTo("Il te reste 5 tours."));
        var waits = Enumerable.Range(0, 4).Select(_ => RadioStyle.Thinking(false, true)).Distinct().ToList();
        Assert.That(waits.Count, Is.GreaterThan(1), "plusieurs formules d'attente");
    }

    [Test]
    public void BatterieUnConseilParSituationPasParAdversaire()
    {
        RaceSnapshot snap(string id) => new()
        {
            HasData = true,
            SessionType = "Race",
            Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = 5 } },
            Player = new PlayerStrategy
            {
                LapsRemaining = 10,
                Hybrid = new HybridDto { ChargePct = 70 },
                Battles = new List<BattleDto> { new() { Id = id, Number = int.Parse(id), Side = "ahead", Gap = 0.5 } },
            },
        };
        ProactiveMessage a = HybridAdvisor.Proactive(snap("51"), snap("51").Drivers[0]);
        ProactiveMessage b = HybridAdvisor.Proactive(snap("93"), snap("93").Drivers[0]);
        Assert.That(a.Key, Is.EqualTo(b.Key), "même clé : le délai entre deux conseils vaut pour tous les adversaires");
        Assert.That(a.CooldownSeconds, Is.GreaterThanOrEqualTo(600));
    }

    // ───── Batterie, énergie et essence pendant le tour ─────

    private static readonly List<TrackSegment> Segments = new()
    {
        new() { Id = "s1", Kind = "straight", Name = "Ligne droite 1 (principale)", Start = 2000, End = 4000 },
        new() { Id = "c1", Kind = "corner", Name = "Virage 5", Start = 5000, End = 5500 },
    };

    /// <summary>Un tour de 10 km : la batterie descend de 20 % sur la ligne droite, remonte de 10 % au virage 5.</summary>
    private static double Battery(double d, double start, double spendFactor = 1.0)
    {
        double b = start;
        b -= 20 * spendFactor * Math.Clamp((d - 2000) / 2000, 0, 1);
        b += 10 * Math.Clamp((d - 5000) / 500, 0, 1);
        return b;
    }

    [Test]
    public void BatteriePendantLeTour()
    {
        var trace = new EnergyLapTrace();
        for (double d = 0; d < 10000; d += 10)
        {
            trace.Update(3, d, Battery(d, 60), 80 - (d / 1000), 50 - (d / 2000), false);
        }

        // Tour suivant, jusqu'à 3 000 m, en dépensant moins dans la ligne droite.
        for (double d = 0; d <= 3000; d += 10)
        {
            trace.Update(4, d, Battery(d, 50, spendFactor: 0.5), 70 - (d / 1000), 45 - (d / 2000), false);
        }

        LiveEnergyDto live = trace.Build(10000, Segments);
        Assert.That(live.Lap, Is.EqualTo(5));
        Assert.That(live.Battery.Now, Is.EqualTo(45).Within(0.2));
        Assert.That(live.Battery.LastLapChange, Is.EqualTo(-10).Within(0.3), "dernier tour : −20 + 10");
        Assert.That(live.Battery.ChangeSoFar, Is.EqualTo(-5).Within(0.3));
        Assert.That(live.Battery.LastLapChangeAtSameDistance, Is.EqualTo(-10).Within(0.3));
        Assert.That(live.Battery.ProjectedLapChange, Is.EqualTo(-5).Within(0.5), "dernier tour corrigé de l'écart actuel");
        Assert.That(live.Battery.Zones.Select(z => z.Name), Is.EqualTo(new[] { "Ligne droite 1 (principale)", "Virage 5" }));

        string radio = HybridAdvisor.LiveBattery(live.Battery);
        Assert.That(radio, Does.Contain("tu la vides le plus dans la ligne droite 1 (principale) (-20 %)").IgnoreCase);
        Assert.That(radio, Does.Contain("tu la recharges le plus au virage 5 (+10 %)"));
        Assert.That(radio, Does.Contain("5 % de mieux qu'au même endroit du tour précédent"));
        Assert.That(radio, Does.Contain("fin de tour estimée à -5 %"));

        // Énergie virtuelle : 1 %/km, comme au tour précédent.
        Assert.That(live.VirtualEnergy.ProjectedLapChange, Is.EqualTo(-10).Within(0.3));
        Assert.That(RaceBriefing.LiveConsumption(live.VirtualEnergy, "%", 1), Does.Contain("autant qu'au même endroit du tour précédent, environ 10,0 % pour le tour"));
    }

    [Test]
    public void PremierTourEstimationParExtrapolation()
    {
        var trace = new EnergyLapTrace();
        for (double d = 0; d <= 6000; d += 10)
        {
            trace.Update(0, d, null, null, 60 - (d / 2000), false); // 0,5 L/km
        }

        LiveEnergyDto live = trace.Build(10000, Segments);
        Assert.That(live.Battery, Is.Null, "voiture sans batterie");
        Assert.That(live.Fuel.ProjectedLapChange, Is.EqualTo(-5).Within(0.1));
        Assert.That(live.Fuel.Extrapolated, Is.True);
        Assert.That(RaceBriefing.LiveConsumption(live.Fuel, "L", 1), Does.Contain("Estimation sur ce tour : environ 5,0 L par tour"));
    }

    [Test]
    public void TourAvecRavitaillementPasDeReference()
    {
        var trace = new EnergyLapTrace();
        for (double d = 0; d < 10000; d += 10)
        {
            double ve = d < 5000 ? 30 - (d / 1000) : 90; // ravitaillement au milieu du tour
            trace.Update(7, d, null, ve, null, false);
        }

        for (double d = 0; d <= 4000; d += 10)
        {
            trace.Update(8, d, null, 90 - (d / 1000), null, false);
        }

        LiveEnergyDto live = trace.Build(10000, Segments);
        Assert.That(live.VirtualEnergy.LastLapChange, Is.Null, "le tour ravitaillé ne sert pas de comparaison");
        Assert.That(live.VirtualEnergy.Extrapolated, Is.True);
    }

    [Test]
    public void FinDeQualificationEtDEssais()
    {
        RaceSnapshot snap(string type) => new()
        {
            HasData = true,
            SessionType = type,
            SessionPhase = "Checkered",
            Drivers = new List<DriverRow>
            {
                new() { Id = "a", ClassId = "h", BestLap = 218.0, Laps = 9 },
                new() { Id = "me", ClassId = "h", IsPlayer = true, BestLap = 218.4, BestLapNumber = 5, Laps = 12 },
                new() { Id = "b", ClassId = "h", BestLap = 219.9, Laps = 8 },
            },
        };
        string q = new RaceFinishRadio().Check(snap("Qualification"));
        Assert.That(q, Does.StartWith("Fin de la qualification : P2, meilleur tour 3:38.400 au tour 5, à 0,40 s de la pole."));
        Assert.That(q, Does.Contain("bravo"));
        string p = RaceFinishRadio.BuildSession(snap("Practice"));
        Assert.That(p, Does.Contain("12 tours bouclés"));
        Assert.That(p, Does.Contain("du meilleur temps"));
        var once = new RaceFinishRadio();
        Assert.That(once.Check(snap("Practice")), Is.Not.Null);
        Assert.That(once.Check(snap("Practice")), Is.Null, "une seule fois");
    }
}


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
}

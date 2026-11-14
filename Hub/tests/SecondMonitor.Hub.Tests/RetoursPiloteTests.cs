namespace SecondMonitor.Hub.Tests;

using System.Text;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Retours du pilote après la séance LMU du 13/11 (carnet 22:00-22:30) : lexique de piste, réponse à « Qu'est-ce qui s'est passé ? »,
/// point d'usure promis, heure, préparation de l'audio pour les modèles « écoute ».
/// </summary>
[TestFixture]
public class RetoursPiloteTests
{
    // ───── Lexique de piste ─────

    [Test]
    public void BooksBooksDevientUnBoxCompris()
    {
        string corrected = RadioLexicon.Correct("Books, books, books, books, on change de pneu.");
        Assert.That(corrected, Does.StartWith("box, box"));
        PitOrder order = PitOrders.Parse(corrected);
        Assert.That(order, Is.Not.Null);
        Assert.That(order.BoxNow, Is.True);
    }

    [Test]
    public void PassionDEnergieDevientGestionDEnergie()
    {
        string corrected = RadioLexicon.Correct("passion d'énergie");
        Assert.That(corrected, Is.EqualTo("gestion d'énergie"));
        Assert.That(RaceBriefing.DetectTopics(corrected), Does.Contain(RaceBriefing.Topic.Energy));
    }

    [TestCase("Quel est mon brake bias ?")]
    [TestCase("c'est quoi ma BB")]
    [TestCase("brèque baïasse")]
    public void BrakeBiasEstLaRepartitionDeFreinage(string heard)
    {
        string understood = RadioLexicon.Understand(RadioLexicon.Correct(heard));
        Assert.That(understood, Does.Contain("répartition de freinage"));
        Assert.That(RaceBriefing.DetectTopics(understood), Does.Contain(RaceBriefing.Topic.Controls));
    }

    [Test]
    public void StintEtDeltaEtFuelSontCompris()
    {
        Assert.That(RaceBriefing.DetectTopics(RadioLexicon.Understand("combien de tours dans ce stint")), Is.Not.Empty);
        Assert.That(RadioLexicon.Understand("le delta avec la voiture devant"), Does.Contain("écart"));
        Assert.That(RaceBriefing.DetectTopics(RadioLexicon.Understand("il me reste combien de fuel")), Does.Contain(RaceBriefing.Topic.Fuel));
    }

    [Test]
    public void CorrectionsDuPiloteSansAccentsNiMajuscules()
    {
        var mine = new[] { new LexiconEntry { Heard = "vexe", Meant = "BB" }, new LexiconEntry { Heard = "les ténèbres", Meant = "les pneus" } };
        Assert.That(RadioLexicon.Correct("Quel est ma Vexe ?", mine), Is.EqualTo("Quel est ma BB ?"));
        Assert.That(RadioLexicon.Correct("Comment ça les tenebres ?", mine), Is.EqualTo("Comment ça les pneus ?"));
        // Pas au milieu d'un mot.
        Assert.That(RadioLexicon.Correct("convexe", mine), Is.EqualTo("convexe"));
    }

    [Test]
    public void PneusMalEntendus()
    {
        Assert.That(RadioLexicon.Correct("Tu penses que je peux rester sur ces plumes?"), Does.Contain("ces pneus"));
        Assert.That(RadioLexicon.Correct("Et les clous vont bien."), Is.EqualTo("Et les pneus vont bien."));
    }

    [Test]
    public void IndiceDeTranscriptionContientLeVocabulaire()
    {
        string hint = RadioLexicon.TranscriptionHint(new[] { new LexiconEntry { Heard = "vexe", Meant = "BB" } });
        foreach (string w in new[] { "box box", "delta", "stint", "undercut", "overcut", "DRS", "ERS", "brake bias", "soft", "medium", "hard", "BB" })
        {
            Assert.That(hint, Does.Contain(w));
        }
    }

    // ───── Heure ─────

    [Test]
    public void HeureDeLOrdinateurPasInventee()
    {
        var now = new DateTime(2026, 11, 13, 22, 14, 0);
        Assert.That(RadioCommands.TimeAnswer("Tu te fous de ma game ? Quelle heure il est ? Tu parles comme si t'étais tout seul.", now), Is.EqualTo("Il est 22 h 14."));
        Assert.That(RadioCommands.TimeAnswer("il reste combien de temps", now), Is.Null);
    }

    // ───── « Qu'est-ce qui s'est passé ? » ─────

    private static PendingQuestion WearQuestion() => new()
    {
        Key = "usure-1",
        Topic = "usure",
        Text = "Usure anormale arrière droit : plus 1,7 % en 4 secondes, en milieu de virage. Survirage au milieu du virage. Essaie : barre antiroulis arrière de 2 à 1. Qu'est-ce qui s'est passé ?",
        Wheel = "RR",
        Phase = "milieu",
        Proposal = "barre antiroulis arrière de 2 à 1",
    };

    private static RaceSnapshot Snap(int laps, double rrWear, double? rrPerLap = 2.0) => new()
    {
        HasData = true,
        Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = laps } },
        Player = new PlayerStrategy
        {
            Wheels = new List<WheelStatus>
            {
                new() { Position = "FL", WearPct = 5 },
                new() { Position = "FR", WearPct = 6 },
                new() { Position = "RL", WearPct = 7 },
                new() { Position = "RR", WearPct = rrWear, WearPerLapPct = rrPerLap },
            },
        },
    };

    [Test]
    public void ExplicationEtQuestionSontSeparees()
    {
        var memory = new RaceMemory();
        memory.Ask(WearQuestion());
        string text = RadioLexicon.Correct("Erreur, euh, sur le vibreur, je suis allé trop loin. J'ai amené trop de vitesse sur le dernier virage et là, tu vois, elle est sortie. Tu penses que je peux rester sur ces plumes?");
        WearAnswer answer = RadioDialog.AnswerWear(memory.Pending(), text, memory, CarContext.From(Snap(3, 10)), Snap(3, 10), 3);
        Assert.That(answer.Reply, Is.Not.Null);
        Assert.That(answer.Reply.Text, Does.Contain("vibreur"));
        Assert.That(answer.Reply.Text, Does.Contain("arrière droit"));
        Assert.That(answer.Question, Is.EqualTo("Tu penses que je peux rester sur ces pneus?"));
        Assert.That(memory.Pending(), Is.Null, "la question de l'ingénieur a sa réponse");
        Assert.That(memory.WearWatches, Has.Count.EqualTo(1), "l'ingénieur refera le point");
    }

    [Test]
    public void ReglageChangeEstNotePasPrisPourUnSimpleCopy()
    {
        var memory = new RaceMemory();
        memory.Ask(WearQuestion());
        WearAnswer answer = RadioDialog.AnswerWear(memory.Pending(), RadioLexicon.Correct("J'ajoute plus un sur le différentiel. Sur la pré-guarance, pardon."), memory, CarContext.From(Snap(2, 10)), Snap(2, 10), 2);
        Assert.That(answer.Question, Is.Null);
        Assert.That(answer.Reply.Text, Does.StartWith("Copy, noté : « J'ajoute plus un sur le différentiel"));
        Assert.That(answer.Reply.Text, Does.Contain("précharge"));
        Assert.That(memory.Entries.Any(e => e.Topic == "réglage"), Is.True);
    }

    [Test]
    public void OkAccepteLeReglagePropose()
    {
        var memory = new RaceMemory();
        memory.Ask(WearQuestion());
        WearAnswer answer = RadioDialog.AnswerWear(memory.Pending(), "ok", memory, CarContext.From(Snap(2, 10)), Snap(2, 10), 2);
        Assert.That(answer.Reply.Text, Does.Contain("barre antiroulis arrière de 2 à 1"));
    }

    [Test]
    public void PatinageEnSortieNEstPasUneSortieDePiste()
    {
        var memory = new RaceMemory();
        memory.Ask(WearQuestion());
        WearAnswer answer = RadioDialog.AnswerWear(memory.Pending(), "j'ai patiné en sortie", memory, CarContext.From(Snap(2, 10)), Snap(2, 10), 2);
        Assert.That(answer.Reply.Text, Does.Not.Contain("sortie de piste"));
    }

    [Test]
    public void SeulementDesQuestionsLaissentLaPipelineRepondre()
    {
        var memory = new RaceMemory();
        memory.Ask(WearQuestion());
        WearAnswer answer = RadioDialog.AnswerWear(memory.Pending(), "est-ce qu'il faut que je change de pneus? Est-ce que c'est plusieurs fois que je glisse?", memory, null, Snap(2, 10), 2);
        Assert.That(answer.Reply, Is.Null);
        Assert.That(answer.Question, Is.Not.Null);
    }

    // ───── Point d'usure promis ─────

    [Test]
    public void PointUsureDeuxToursPlusTard()
    {
        var memory = new RaceMemory();
        memory.WatchWear(new WearWatch { Wheel = "RR", Lap = 3, StartWearPct = 10, BaselinePerLap = 2.0 });
        Assert.That(WearFollowUp.Check(Snap(4, 11), memory, DateTime.UtcNow), Is.Null, "un seul tour : trop tôt");
        string point = WearFollowUp.Check(Snap(5, 13), memory, DateTime.UtcNow);
        Assert.That(point, Does.Contain("arrière droit"));
        Assert.That(point, Does.Contain("revenu à la normale"));
        Assert.That(memory.WearWatches, Is.Empty, "dit une seule fois");
    }

    [Test]
    public void PointUsureEncoreForteRappelleLeReglage()
    {
        var memory = new RaceMemory();
        memory.WatchWear(new WearWatch { Wheel = "RR", Lap = 3, StartWearPct = 10, BaselinePerLap = 2.0, Proposal = "TC de 6 à 7" });
        string point = WearFollowUp.Check(Snap(5, 18), memory, DateTime.UtcNow);
        Assert.That(point, Does.Contain("4,0 % par tour"));
        Assert.That(point, Does.Contain("TC de 6 à 7"));
    }

    [Test]
    public void PneusChangesAnnulentLePoint()
    {
        var memory = new RaceMemory();
        memory.WatchWear(new WearWatch { Wheel = "RR", Lap = 3, StartWearPct = 10, BaselinePerLap = 2.0 });
        Assert.That(WearFollowUp.Check(Snap(5, 1), memory, DateTime.UtcNow), Is.Null);
        Assert.That(memory.WearWatches, Is.Empty);
    }

    // ───── Audio pour le modèle « écoute » ─────

    private static byte[] Wav(int rate, int channels, int bits, IEnumerable<float> mono, bool floating = false)
    {
        var data = mono.ToList();
        int bytes = bits / 8;
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + (data.Count * channels * bytes));
        w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);
        w.Write((short)(floating ? 3 : 1));
        w.Write((short)channels);
        w.Write(rate);
        w.Write(rate * channels * bytes);
        w.Write((short)(channels * bytes));
        w.Write((short)bits);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(data.Count * channels * bytes);
        foreach (float s in data)
        {
            for (int c = 0; c < channels; c++)
            {
                if (floating)
                {
                    w.Write(s);
                }
                else if (bits == 16)
                {
                    w.Write((short)(s * 32767));
                }
                else
                {
                    int v = (int)(s * 8388607);
                    w.Write((byte)v);
                    w.Write((byte)(v >> 8));
                    w.Write((byte)(v >> 16));
                }
            }
        }

        w.Flush();
        return ms.ToArray();
    }

    private static IEnumerable<float> Tone(int rate, double seconds) =>
        Enumerable.Range(0, (int)(rate * seconds)).Select(i => (float)(0.3 * Math.Sin(2 * Math.PI * 220 * i / rate)));

    private static IEnumerable<float> Silence(int rate, double seconds) => Enumerable.Repeat(0f, (int)(rate * seconds));

    [TestCase(48000, 2, 16, false)]
    [TestCase(44100, 1, 24, false)]
    [TestCase(48000, 2, 32, true)]
    public void ToutWavDevient16kHzMonoSansSilences(int rate, int channels, int bits, bool floating)
    {
        byte[] wav = Wav(rate, channels, bits, Silence(rate, 1).Concat(Tone(rate, 2)).Concat(Silence(rate, 1.5)), floating);
        List<byte[]> chunks = WavAudio.ForTranscription(wav, 28);
        Assert.That(chunks, Has.Count.EqualTo(1));
        var decoded = WavAudio.Decode(chunks[0]);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Value.Rate, Is.EqualTo(16000));
        Assert.That(chunks[0][22], Is.EqualTo(1), "mono");
        Assert.That(chunks[0][34], Is.EqualTo(16), "16 bits");
        Assert.That(WavAudio.Duration(chunks[0]), Is.InRange(2.0, 2.7), "2 s de voix + marges, silences retirés");
    }

    [Test]
    public void LongEnregistrementDecoupeSurLesSilences()
    {
        int rate = 16000;
        var audio = Enumerable.Empty<float>();
        for (int i = 0; i < 7; i++)
        {
            audio = audio.Concat(Tone(rate, 9.5)).Concat(Silence(rate, 0.5));
        }

        List<byte[]> chunks = WavAudio.ForTranscription(Wav(rate, 1, 16, audio), 28);
        Assert.That(chunks.Count, Is.InRange(3, 4));
        Assert.That(chunks.All(c => WavAudio.Duration(c) <= 28.01), Is.True);
        Assert.That(chunks.Sum(WavAudio.Duration), Is.EqualTo(69.75).Within(1.0), "rien de perdu");
    }

    [Test]
    public void SilenceSeulRienAEnvoyer()
    {
        Assert.That(WavAudio.ForTranscription(Wav(16000, 1, 16, Silence(16000, 2)), 28), Is.Empty);
    }

    [Test]
    public void PasUnWavResteTelQuel()
    {
        Assert.That(WavAudio.ForTranscription(Encoding.ASCII.GetBytes("OggS................"), 28), Is.Null);
    }
}

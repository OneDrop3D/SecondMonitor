namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Radio : questions réellement posées pendant les essais, commandes vocales, ingénieur proactif, mémoire.</summary>
[TestFixture]
public class RadioTests
{
    private static RaceEngine _engine;
    private static RaceSnapshot _snap;
    private static BriefingExtras _extras;
    private static string _dir;

    private static readonly RadioCommands.ModelCandidate[] Models =
    {
        new("lfm3-omni:2b", "lfm3-omni:2b", "Mac", new[] { "completion", "audio", "speech" }),
        new("lfm3-audio:2b", "lfm3-audio:2b", "Mac", new[] { "completion", "audio", "speech" }),
        new("mistral-7b-instruct-v0.3-q4_k_m:latest", "mistral-7b-instruct-v0.3-q4_k_m:latest", "Mac", new[] { "completion" }),
        new("qwen3:1.7b@raspberry", "qwen3:1.7b", "raspberry", new[] { "completion" }),
    };

    [OneTimeSetUp]
    public void RunRace()
    {
        _dir = Path.Combine(Path.GetTempPath(), "smhub-radio-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator(seed: 3);
        _engine = new RaceEngine(_dir);
        for (double t = 0; t < 30 * 60; t += 0.1)
        {
            _engine.Process(sim.Step(0.1));
        }

        _snap = _engine.BuildSnapshot();
        _extras = new BriefingExtras { Speeds = _engine.BuildSpeedAnalysis(), MicroSectors = _engine.BuildMicroSectors(), Weather = _engine.BuildWeather(), Memory = new RaceMemory() };
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    // Questions tirées de l'historique radio réel, avec le sujet attendu.
    [TestCase("quelle est ma vitesse de pointe", RaceBriefing.Topic.Speed)]
    [TestCase("quelle est ma vitesse maximmal dans les ligne droite", RaceBriefing.Topic.Speed)]
    [TestCase("ou je perd du temps", RaceBriefing.Topic.Performance)]
    [TestCase("qu'elle pneu avec la pluie qui arrive , on devras mettre des pneu pluie ou pas", RaceBriefing.Topic.Rain)]
    [TestCase("dis moi quans on passera au wet", RaceBriefing.Topic.Rain)]
    [TestCase("combin de tour resten", RaceBriefing.Topic.Session)]
    [TestCase("Il me reste combien de tours d'essence ?", RaceBriefing.Topic.Fuel)]
    [TestCase("je boxe dans combien de temps", RaceBriefing.Topic.Pit)]
    [TestCase("combien de temps devant moi", RaceBriefing.Topic.Gap)]
    public void RealQuestions_GoToTheRightTopic(string question, RaceBriefing.Topic expected)
    {
        Assert.That(RaceBriefing.DetectTopics(question).First(), Is.EqualTo(expected));
    }

    [Test]
    public void Answers_UseClassStandings()
    {
        string position = RaceBriefing.Prepare(_snap, "Quelle est ma position ?", _extras).Suggestion;
        Assert.That(position, Does.Contain("dans ta catégorie"));
        Assert.That(position, Does.Not.Contain("général"));
    }

    [Test]
    public void Answers_TopSpeed_And_TimeLoss()
    {
        Assert.That(RaceBriefing.Prepare(_snap, "quelle est ma vitesse de pointe", _extras).Suggestion, Does.Contain("km/h"));
        Assert.That(RaceBriefing.Prepare(_snap, "ou je perd du temps", _extras).Suggestion, Does.Contain("micro-secteur").Or.Contain("au niveau des meilleurs"));
        Assert.That(RaceBriefing.Prepare(_snap, "pneu pluie ?", _extras).Suggestion, Does.Contain("pneus"));
    }

    [Test]
    public void PitAnswer_GivesTime()
    {
        string pit = RaceBriefing.Prepare(_snap, "je boxe dans combien de temps", _extras).Suggestion;
        Assert.That(pit, Does.Contain("Rentre au stand").Or.Contain("Pas besoin"));
    }

    [TestCase("Merci.", "Copy")]
    [TestCase("ok", "Copy")]
    [TestCase("bien reçu", "Copy")]
    public void SmallTalk_IsAnsweredInFrench(string text, string expected)
    {
        Assert.That(RadioCommands.SmallTalk(text), Does.StartWith(expected));
    }

    [Test]
    public void SmallTalk_IgnoresRealQuestions()
    {
        Assert.That(RadioCommands.SmallTalk("ok je rentre quand ?"), Is.Null);
    }

    [TestCase("You're welcome! If you have any more questions, feel free to ask.", true)]
    [TestCase("Le document ne mentionne pas de vitesse maximale.", true)]
    [TestCase("Le texte ne mentionne pas l'heure.", true)]
    [TestCase("Rentre au stand au tour 12.", false)]
    public void UnusableAnswers_AreDetected(string answer, bool unusable)
    {
        Assert.That(RadioCommands.IsUnusableAnswer(answer), Is.EqualTo(unusable));
    }

    [Test]
    public void ModelSwitch_ByVoice()
    {
        var m = RadioCommands.ParseModelSwitch("on passe à mistral", Models);
        Assert.That(m.Role, Is.EqualTo("think"));
        Assert.That(m.ModelId, Does.StartWith("mistral"));
        Assert.That(m.Ambiguous, Is.Empty);

        var voice = RadioCommands.ParseModelSwitch("passe sur omni pour la voix", Models);
        Assert.That(voice.Role, Is.EqualTo("speak"));
        Assert.That(voice.ModelId, Is.EqualTo("lfm3-omni:2b"));

        var pi = RadioCommands.ParseModelSwitch("utilise qwen sur le raspberry", Models);
        Assert.That(pi.ModelId, Is.EqualTo("qwen3:1.7b@raspberry"));
    }

    [Test]
    public void ModelSwitch_AmbiguousName_AsksToRepeat()
    {
        var m = RadioCommands.ParseModelSwitch("on passe à omniaudio", Models);
        Assert.That(m.Ambiguous, Has.Count.EqualTo(2));
    }

    [Test]
    public void ModelSwitch_Alias()
    {
        var m = RadioCommands.ParseModelSwitch("on passe à omniaudio", Models, new Dictionary<string, string> { ["omniaudio"] = "lfm3-omni:2b" });
        Assert.That(m.ModelId, Is.EqualTo("lfm3-omni:2b"));
        Assert.That(m.Ambiguous, Is.Empty);
    }

    [TestCase("je passe au stand")]
    [TestCase("on change les pneus")]
    [TestCase("passe en pneus pluie")]
    public void ModelSwitch_IgnoresRacePhrases(string text)
    {
        Assert.That(RadioCommands.ParseModelSwitch(text, Models), Is.Null);
    }

    [TestCase("oui ça va, on peut allonger", true)]
    [TestCase("non l'arrière glisse trop", false)]
    [TestCase("je sais pas trop", null)]
    public void YesNo(string text, bool? expected)
    {
        Assert.That(RadioCommands.YesNo(text), Is.EqualTo(expected));
    }

    [Test]
    public void Feedback_IsRemembered_AndUsedInFacts()
    {
        Assert.That(RadioCommands.IsFeedback("l'arrière glisse beaucoup en sortie de virage"), Is.True);
        Assert.That(RadioCommands.IsFeedback("quand est-ce que je rentre ?"), Is.False);
        var memory = new RaceMemory();
        memory.Add("pilote", "retour", "« l'arrière glisse »", 12);
        string facts = RaceBriefing.Prepare(_snap, "État des pneus ?", new BriefingExtras { Memory = memory }).Facts;
        Assert.That(facts, Does.Contain("MÉMOIRE DE LA COURSE").And.Contain("l'arrière glisse"));
    }

    [Test]
    public void StintExtension_AnswerYes_PlansTheStop()
    {
        var memory = new RaceMemory();
        var q = new PendingQuestion { Topic = "relais", PlannedLap = 20, ProposedLap = 24 };
        var (reply, note) = ProactiveEngineer.HandleAnswer(q, "oui ça tient, on y va", memory);
        Assert.That(reply, Does.Contain("tour 24"));
        Assert.That(memory.PlannedStopLap, Is.EqualTo(24));
        Assert.That(note, Does.Contain("allongé"));
        Assert.That(RaceBriefing.Prepare(_snap, "je rentre quand ?", new BriefingExtras { Memory = memory }).Suggestion, Does.Contain("décidé ensemble").Or.Contain("Pas besoin"));
    }

    [Test]
    public void Proactive_SpeaksDuringTheDemoRace()
    {
        // Course de démo complète (avec l'averse de la 35e minute) : l'IA doit intervenir, sans saturer la radio.
        var engine = new RaceEngine(Path.Combine(_dir, "proactive"));
        var sim = new DemoRaceSimulator(seed: 9);
        var rules = new ProactiveEngineer();
        var memory = new RaceMemory();
        var messages = new List<ProactiveMessage>();
        DateTime clock = DateTime.UtcNow;
        for (double t = 0; t < 55 * 60; t += 0.2)
        {
            engine.Process(sim.Step(0.2));
            if ((int)(t * 5) % 15 != 0)
            {
                continue;
            }

            clock = clock.AddSeconds(3);
            ProactiveMessage m = rules.Evaluate(engine.BuildSnapshot(), engine.BuildWeather(), memory, new ProactiveOptions(), clock);
            if (m != null)
            {
                messages.Add(m);
                memory.ClearPending();
            }
        }

        Assert.That(messages, Is.Not.Empty, "L'ingénieur n'a jamais pris la parole");
        Assert.That(messages.Count, Is.LessThan(60), "Trop d'interventions");
        Assert.That(messages.Any(m => m.Topic == "stand"), "Pas d'annonce de box");
        Assert.That(messages.All(m => !string.IsNullOrWhiteSpace(m.Text) && !m.Text.Contains("NaN")));
    }
}

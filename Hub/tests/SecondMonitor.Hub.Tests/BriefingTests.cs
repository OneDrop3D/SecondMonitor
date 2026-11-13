namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

[TestFixture]
public class BriefingTests
{
    private static RaceSnapshot _snap;
    private static BriefingExtras _extras;

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
        _extras = new BriefingExtras { Speeds = engine.BuildSpeedAnalysis(), MicroSectors = engine.BuildMicroSectors(), Weather = engine.BuildWeather(), Laps = engine.PlayerLaps() };
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
        Assert.That(r.Suggestion, Does.Match(@"\d:\d\d\.\d")); // temps au tour « 2:01.5 », lu « deux minutes, une seconde cinq »
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

    // ───── Retours réels de l'utilisateur (démo) : réponses calculées, instantanées et courtes ─────

    private static int Sentences(string text) => System.Text.RegularExpressions.Regex.Matches(text, @"[.!?](\s|$)").Count;

    private static RaceSnapshot Clone(RaceSnapshot s) =>
        System.Text.Json.JsonSerializer.Deserialize<RaceSnapshot>(System.Text.Json.JsonSerializer.Serialize(s));

    [TestCase("Mon meilleur tour", "Meilleur tour ")]
    [TestCase("pire tour", "Pire tour ")]
    [TestCase("c'était quoi mon dernier tour ?", "Dernier tour ")]
    [TestCase("quelle est ma moyenne ?", "Moyenne ")]
    [TestCase("je suis régulier ?", "Régularité")]
    [TestCase("quelle est ma vmax comparée aux autres", "Vitesse max ")]
    public void UserQuestions_GetAnInstantShortComputedAnswer(string question, string expected)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        BriefingResult r = RaceBriefing.Prepare(_snap, question, _extras);
        sw.Stop();
        TestContext.Out.WriteLine($"{question} → {r.Suggestion} ({sw.ElapsedMilliseconds} ms)");
        Assert.That(r.Suggestion, Does.StartWith(expected));
        Assert.That(r.Suggestion, Does.Not.Contain("pas cette information"));
        Assert.That(r.Suggestion.Length, Is.LessThan(230), "réponse trop longue pour la radio");
        Assert.That(Sentences(r.Suggestion), Is.LessThanOrEqualTo(2));
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500));
    }

    [Test]
    public void BestAndWorstLap_GiveLapNumberAndGap()
    {
        Assert.That(RaceBriefing.Prepare(_snap, "Mon meilleur tour", _extras).Suggestion, Does.Match(@"^Meilleur tour \d:\d\d\.\d{3} au tour \d+"));
        Assert.That(RaceBriefing.Prepare(_snap, "pire tour", _extras).Suggestion, Does.Match(@"^Pire tour \d:\d\d\.\d{3} au tour \d+, à [\d,]+ s de votre meilleur"));
    }

    [Test]
    public void TopSpeed_IsGivenInTheRequestedFormat()
    {
        string a = RaceBriefing.Prepare(_snap, "quelle est ma vmax comparée aux autres", _extras).Suggestion;
        Assert.That(a, Does.Match(@"^Vitesse max \d+ km/h au tour \d+, en moyenne \d+ km/h ; (le plus rapide de la catégorie est le numéro \d+ à \d+ km/h|c'est la meilleure de la catégorie)"));
    }

    private static (RaceSnapshot Snap, SpeedAnalysisDto Speeds) TopSpeedDeficit(double myCornerExit)
    {
        RaceSnapshot snap = Clone(_snap);
        DriverRow me = snap.Drivers.First(d => d.IsPlayer);
        DriverRow rival = snap.Drivers.First(d => !d.IsPlayer && d.ClassId == me.ClassId);
        var corner = new TrackSegment { Id = "c1", Kind = "corner", Name = "Virage 6", Apex = 100, Start = 50, End = 150 };
        var straight = new TrackSegment { Id = "s1", Kind = "straight", Name = "Ligne droite 2 (principale)", Apex = 600, Start = 150, End = 900, IsMain = true };
        var speeds = new SpeedAnalysisDto
        {
            Segments = new List<TrackSegment> { corner, straight },
            Drivers = new List<DriverSpeedDto>
            {
                new() { Id = me.Id, TopSpeedBest = 200, TopSpeedBestLap = 4, TopSpeedAverage = 198, LapsAnalysed = 5, Best = new() { ["c1"] = myCornerExit, ["s1"] = 200 } },
                new() { Id = rival.Id, TopSpeedBest = 208, LapsAnalysed = 5, Best = new() { ["c1"] = 97, ["s1"] = 208 } },
            },
        };
        snap.Weather.Wetness = 0;
        return (snap, speeds);
    }

    [Test]
    public void TopSpeedDeficit_SlowExit_AdvisesTheExitOfThePreviousCorner()
    {
        var (snap, speeds) = TopSpeedDeficit(myCornerExit: 90);
        BriefingResult r = RaceBriefing.Prepare(snap, "quelle est ma vmax comparée aux autres", new BriefingExtras { Speeds = speeds });
        TestContext.Out.WriteLine(r.Suggestion);
        Assert.That(r.Suggestion, Does.StartWith("Vitesse max 200 km/h au tour 4, en moyenne 198 km/h ; le plus rapide de la catégorie est le numéro"));
        Assert.That(r.Suggestion, Does.Contain("sortie du virage 6").Or.Contain("sortez moins vite du virage 6"));
        Assert.That(r.Proposal?.Kind, Is.EqualTo("pilotage"));
    }

    [Test]
    public void TopSpeedDeficit_GoodExit_ProposesLessRearWing_AndYesIsLinkedToIt()
    {
        var (snap, speeds) = TopSpeedDeficit(myCornerExit: 97);
        BriefingResult r = RaceBriefing.Prepare(snap, "quelle est ma vmax comparée aux autres", new BriefingExtras { Speeds = speeds });
        TestContext.Out.WriteLine(r.Suggestion);
        Assert.That(r.Suggestion, Does.Contain("aileron arrière en moins"));
        var memory = new RaceMemory();
        RadioDialog.Offered(memory, r.Proposal, "vitesse", "Vitesse de pointe", ExchangeCategorizer.RaceInfo);
        DialogReply yes = RadioDialog.FollowUp("oui", memory, new CarContext(), 8);
        Assert.That(yes.Text, Does.Contain("Noté pour le prochain arrêt").And.Contain("aileron"));
        Assert.That(yes.Category, Is.EqualTo(ExchangeCategorizer.CarSetup));
    }

    // ───── Base « symptôme → réglages » ─────

    private static CarContext Gt3() => new() { Game = "ACC", CarClass = "GT3", HasAbs = true, HasTc = true, HasEngineMap = true, StintLaps = 6, WorstWearPct = 20 };

    [Test]
    public void Understeer_GetsAdviceAtOnce_NeverAPlainAcknowledgement()
    {
        var memory = new RaceMemory();
        DialogReply r = RadioDialog.Feedback("Pilote : j'ai du sous-virage", memory, Gt3(), _snap, 5);
        TestContext.Out.WriteLine(r.Text);
        Assert.That(r.Kind, Is.EqualTo("conseil"));
        Assert.That(r.Text, Does.StartWith("Sous-virage"));
        Assert.That(r.Text, Does.Contain("répartition de freinage"));
        Assert.That(r.Text, Does.Not.Contain("Reçu, c'est noté"));
        Assert.That(memory.Entries.Last().Symptom, Is.EqualTo("understeer"));

        // « C'est en sortie » se rattache au sous-virage.
        DialogReply exit = RadioDialog.FollowUp("c'est en sortie", memory, Gt3(), 5);
        TestContext.Out.WriteLine(exit.Text);
        Assert.That(exit.Text, Does.StartWith("Sous-virage en sortie"));
    }

    [Test]
    public void InsideWheelLock_AtACorner_GivesTheDrivingFixThenASetting()
    {
        DialogReply r = RadioDialog.Feedback("Je bloque au virage 1 à chaque fois la roue intérieure", new RaceMemory(), Gt3(), _snap, 5);
        TestContext.Out.WriteLine(r.Text);
        Assert.That(r.Kind, Is.EqualTo("conseil"));
        Assert.That(r.Text, Does.Contain("roue intérieure au virage 1"));
        Assert.That(r.Text, Does.Contain("relâchez le frein plus tôt"));
        Assert.That(r.Category, Is.EqualTo(ExchangeCategorizer.RaceSettings));
    }

    [Test]
    public void FrontLock_ProposesBrakeBiasBack_NoMeansCopy_AndItIsNotProposedAgain()
    {
        var memory = new RaceMemory();
        DialogReply r = RadioDialog.Feedback("je bloque les roues avant partout", memory, Gt3(), _snap, 5);
        TestContext.Out.WriteLine(r.Text);
        Assert.That(r.Text, Does.Contain("reculer la répartition de freinage"));

        DialogReply no = RadioDialog.FollowUp("non", memory, Gt3(), 5);
        Assert.That(no.Text, Is.EqualTo("Copy, compris."));
        Assert.That(memory.Thread.IsRefused("bias-back"));

        DialogReply again = RadioDialog.Feedback("ça bloque encore l'avant", memory, Gt3(), _snap, 7);
        TestContext.Out.WriteLine(again.Text);
        Assert.That(again.Text, Does.Not.Contain("répartition"));
        Assert.That(again.Text, Does.Contain("ABS"));
    }

    [Test]
    public void FollowUps_YesAndSomethingElse()
    {
        var memory = new RaceMemory();
        RadioDialog.Feedback("l'arrière glisse en sortie", memory, Gt3(), _snap, 3);
        DialogReply yes = RadioDialog.FollowUp("vas-y", memory, Gt3(), 3);
        Assert.That(yes.Text, Does.StartWith("Bien reçu : montez le TC"));
        DialogReply more = RadioDialog.FollowUp("autre chose ?", memory, Gt3(), 3);
        TestContext.Out.WriteLine(more.Text);
        Assert.That(more.Text, Does.StartWith("Sinon"));
        DialogReply better = RadioDialog.FollowUp("c'est mieux", memory, Gt3(), 4);
        Assert.That(better.Text, Does.Contain("on garde"));
        Assert.That(memory.Thread.Decisions.Any(d => d.Decision == "efficace" && d.Key == "tc-up"));
    }

    [Test]
    public void ShortYes_WithoutAnyProposal_IsNotAFollowUp()
    {
        Assert.That(RadioDialog.FollowUp("ok", new RaceMemory(), Gt3(), 3), Is.Null);
    }

    [Test]
    public void Advice_DependsOnTheCarAndConditions()
    {
        var wet = Gt3();
        wet.Wet = true;
        wet.WetnessPct = 40;
        SetupAdvisor.Advice rain = SetupAdvisor.Advise("l'arrière décroche en sortie", wet);
        Assert.That(rain.Text, Does.StartWith("Piste mouillée"));
        Assert.That(rain.Main.Key, Is.EqualTo("tc-up"));

        var f1 = new CarContext { Game = "F1", CarClass = "F1", HasDiffInRace = true, StintLaps = 5 };
        SetupAdvisor.Advice diff = SetupAdvisor.Advise("l'arrière décroche en sortie", f1);
        Assert.That(diff.Main.Key, Is.EqualTo("diff-lock"));

        var hyper = new CarContext { Game = "LMU", CarClass = "Hypercar", HasTc = true, StintLaps = 5 };
        SetupAdvisor.Advice lockHyper = SetupAdvisor.Advise("je bloque l'avant", hyper);
        Assert.That(lockHyper.Proposals.Select(p => p.Key), Does.Not.Contain("abs-up"));

        var cold = Gt3();
        cold.StintLaps = 0;
        cold.TyresCold = true;
        Assert.That(SetupAdvisor.Advise("j'ai du survirage en entrée", cold).Text, Does.StartWith("Pneus encore froids"));

        SetupAdvisor.Advice mid = SetupAdvisor.Advise("sous-virage au milieu du virage", Gt3());
        Assert.That(mid.Main.Kind, Is.EqualTo("setup"));
        Assert.That(mid.Category, Is.EqualTo(ExchangeCategorizer.CarSetup));
    }

    [TestCase("comment chauffent mes pneus ?", null)]
    [TestCase("j'ai du sous-virage", "understeer")]
    [TestCase("l'arrière glisse beaucoup en sortie de virage", "oversteer")]
    [TestCase("ça patine en sortie des virages lents", "wheelspin")]
    [TestCase("la voiture est instable au freinage", "brake-unstable")]
    [TestCase("ça rebondit sur les vibreurs", "bouncing")]
    [TestCase("les pneus surchauffent", "tyres-hot")]
    [TestCase("je bloque l'arrière au freinage", "lock-rear")]
    [TestCase("quelle est ma position ?", null)]
    public void Symptoms_AreRecognised(string text, string expected)
    {
        Assert.That(SetupAdvisor.DetectSymptom(text), Is.EqualTo(expected));
    }

    [TestCase("oui", ConversationThread.FollowUpKind.Accept)]
    [TestCase("fais-le", ConversationThread.FollowUpKind.Accept)]
    [TestCase("non", ConversationThread.FollowUpKind.Refuse)]
    [TestCase("non merci on garde comme ça", ConversationThread.FollowUpKind.Refuse)]
    [TestCase("autre chose ?", ConversationThread.FollowUpKind.More)]
    [TestCase("c'est mieux", ConversationThread.FollowUpKind.Better)]
    [TestCase("toujours pareil", ConversationThread.FollowUpKind.Worse)]
    [TestCase("plutôt en sortie", ConversationThread.FollowUpKind.Phase)]
    [TestCase("quand est-ce que je rentre au stand", ConversationThread.FollowUpKind.None)]
    public void FollowUps_AreClassified(string text, ConversationThread.FollowUpKind expected)
    {
        Assert.That(ConversationThread.Classify(text), Is.EqualTo(expected));
    }

    // ───── Catégories du carnet ─────

    [TestCase("Combien de tours d'essence ?", ExchangeCategorizer.Strategy)]
    [TestCase("Je rentre quand ?", ExchangeCategorizer.Strategy)]
    [TestCase("Mes pressions sont bonnes ?", ExchangeCategorizer.CarSetup)]
    [TestCase("Mon meilleur tour", ExchangeCategorizer.RaceInfo)]
    [TestCase("quelle est ma vmax comparée aux autres", ExchangeCategorizer.RaceInfo)]
    [TestCase("batterie ?", ExchangeCategorizer.RaceSettings)]
    [TestCase("Écart avec la voiture derrière ?", ExchangeCategorizer.RaceInfo)]
    public void Exchanges_AreCategorised(string question, string expected)
    {
        Assert.That(ExchangeCategorizer.Categorize(question, RaceBriefing.DetectTopics(question)), Is.EqualTo(expected));
    }

    [TestCase("Réglages de course.", ExchangeCategorizer.RaceSettings)]
    [TestCase("Catégorie : Réglages de la voiture", ExchangeCategorizer.CarSetup)]
    [TestCase("stratégie", ExchangeCategorizer.Strategy)]
    [TestCase("Informations course", ExchangeCategorizer.RaceInfo)]
    public void ModelCategory_IsParsed(string output, string expected)
    {
        Assert.That(ExchangeCategorizer.Parse(output), Is.EqualTo(expected));
    }

    // ───── Batterie (hybrides) ─────

    [Test]
    public void Battery_Questions_GetComputedAdvice()
    {
        RaceSnapshot snap = Clone(_snap);
        snap.Player.Hybrid = new HybridDto { ChargePct = 10, Mode = "Balanced", System = "F1", LastLapStartPct = 30, LastLapEndPct = 12 };
        string low = RaceBriefing.Prepare(snap, "batterie ?", _extras).Suggestion;
        TestContext.Out.WriteLine(low);
        Assert.That(low, Does.StartWith("Batterie 10 %, mode bas"));
        Assert.That(low, Does.Contain("Batterie basse"));

        string where = RaceBriefing.Prepare(snap, "je déploie où ?", _extras).Suggestion;
        TestContext.Out.WriteLine(where);
        Assert.That(where, Does.StartWith("Déployez dans").Or.StartWith("Je n'ai pas encore le profil"));

        RaceSnapshot noHybrid = Clone(_snap);
        noHybrid.Player.Hybrid = null;
        Assert.That(RaceBriefing.Prepare(noHybrid, "batterie ?", _extras).Suggestion, Does.StartWith("Pas de batterie"));
        Assert.That(RaceBriefing.Missing("et la batterie ?", noHybrid), Does.Contain("batterie"));
    }

    [Test]
    public void Battery_LastLap_TellsToEmptyIt()
    {
        RaceSnapshot snap = Clone(_snap);
        DriverRow me = snap.Drivers.First(d => d.IsPlayer);
        me.InPits = false;
        snap.SessionType = "Race";
        snap.Player.LapsRemaining = 0.8;
        snap.Player.Hybrid = new HybridDto { ChargePct = 60, Mode = "Attack", System = "F1" };
        Assert.That(HybridAdvisor.Proactive(snap, me)?.Text, Does.StartWith("Dernier tour : videz la batterie"));
    }

    [Test]
    public void HybridTracker_RecordsTheLapAndWhereTheBatteryWasEmpty()
    {
        var tracker = new HybridTracker();
        var player = new SecondMonitor.DataModel.Snapshot.Drivers.DriverInfo { CarInfo = new SecondMonitor.DataModel.Snapshot.Systems.CarInfo() };
        player.CarInfo.HybridSystem.HybridSystemMode = SecondMonitor.DataModel.Snapshot.Systems.HybridSystemMode.Attack;
        double t = 0;
        foreach (var (lap, dist, charge) in new[] { (1, 0.0, 0.8), (1, 1000.0, 0.4), (1, 2500.0, 0.01), (1, 3000.0, 0.05), (2, 10.0, 0.1) })
        {
            player.CompletedLaps = lap;
            player.LapDistance = dist;
            player.CarInfo.HybridSystem.RemainingChargePercentage = charge;
            tracker.Update(player, t += 1);
        }

        HybridDto dto = tracker.Fill(null, player.CarInfo.HybridSystem, "F1 2024");
        Assert.That(dto.LastLapStartPct, Is.EqualTo(80));
        Assert.That(dto.LastLapMinPct, Is.EqualTo(1));
        Assert.That(dto.LastLapEmptyAt, Is.EqualTo(2500));
        Assert.That(dto.System, Is.EqualTo("F1"));
        Assert.That(dto.ChargeChange, Is.EqualTo("harvest"));
    }

    // ───── Fin de course et carnet ─────

    [Test]
    public void RaceFinish_RadioGivesTheResult()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smhub-finish-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator(seed: 5, raceDurationSeconds: 420);
        var engine = new RaceEngine(dir);
        var radio = new RaceFinishRadio();
        var said = new List<string>();
        for (double t = 0; t < 12 * 60; t += 0.2)
        {
            engine.Process(sim.Step(0.2));
            if ((int)(t * 5) % 10 == 0 && radio.Check(engine.BuildSnapshot()) is string m)
            {
                said.Add(m);
            }
        }

        Directory.Delete(dir, true);
        TestContext.Out.WriteLine(string.Join("\n", said));
        Assert.That(said, Has.Count.EqualTo(1), "une seule radio d'arrivée");
        Assert.That(said[0], Does.Match(@"^Drapeau à damier ! P\d+"));
        Assert.That(said[0], Does.Contain("Meilleur tour"));
    }

    [Test]
    public void Carnet_RecordsTheSession_AndBuildsARecapWithoutAi()
    {
        var memory = new RaceMemory();
        var ctxNew = Gt3();
        ctxNew.StintLaps = 1;
        RadioDialog.Feedback("j'ai du sous-virage", memory, ctxNew, _snap, 2);
        var ctxOld = Gt3();
        ctxOld.WorstWearPct = 70;
        ctxOld.StintLaps = 18;
        RadioDialog.Feedback("l'arrière glisse en sortie", memory, ctxOld, _snap, 18);
        RadioDialog.FollowUp("non", memory, ctxOld, 18);
        RadioDialog.Feedback("je bloque l'avant partout", memory, Gt3(), _snap, 9);

        CarnetSession c = CarnetBuilder.Create(_snap, new DateTime(2026, 11, 13, 20, 0, 0));
        c.Radios.Add(new CarnetRadio { At = DateTime.Now, Lap = 2, From = "pilote", Question = "j'ai du sous-virage", Answer = "Sous-virage : …", Kind = "conseil", Category = ExchangeCategorizer.CarSetup });
        c.Radios.Add(new CarnetRadio { At = DateTime.Now, Lap = 5, From = "pilote", Question = "Combien de tours d'essence ?", Answer = "…", Kind = "calculée", Category = ExchangeCategorizer.Strategy });
        CarnetBuilder.Update(c, _snap, _extras.Laps, memory);
        CarnetRecap recap = CarnetBuilder.Recap(c);
        foreach (RecapSection s in recap.Sections)
        {
            TestContext.Out.WriteLine($"{s.Title} : {s.Text}");
        }

        Assert.That(recap.Sections.Select(s => s.Title), Is.EquivalentTo(new[] { "Déroulé", "Radios", "Pneus", "Freinage", "Comportement", "Setup à essayer", "Conclusion" }));
        Assert.That(recap.Sections.Single(s => s.Title == "Comportement").Text, Does.Contain("sous-vireuse en pneus neufs").IgnoreCase.And.Contain("survireuse en pneus usés"));
        Assert.That(recap.Sections.Single(s => s.Title == "Freinage").Text, Does.Contain("Blocage de l'avant"));
        Assert.That(recap.Sections.Single(s => s.Title == "Radios").Text, Does.Contain("Stratégie 1"));

        var dir = Path.Combine(Path.GetTempPath(), "smhub-carnet-" + Guid.NewGuid().ToString("N"));
        var store = new CarnetStore(dir);
        c.Recap = recap;
        store.Save(c);
        CarnetSession back = store.Load(c.Id);
        Assert.That(back.Radios, Has.Count.EqualTo(2));
        Assert.That(back.Notes.Count(n => n.Symptom != null && n.Topic == "retour"), Is.EqualTo(3));
        Assert.That(store.List().Single().HasRecap);
        Assert.That(store.Load("../etc"), Is.Null);
        Directory.Delete(dir, true);
    }

    [TestCase("quel est mon réglage d'aileron actuel ?", "réglages de la voiture")]
    [TestCase("quelle est l'essence du numéro 54 ?", "essence")]
    public void UnavailableData_IsSaidInOneSentence(string question, string expected)
    {
        string a = RaceBriefing.Prepare(_snap, question, _extras).Suggestion;
        TestContext.Out.WriteLine(a);
        Assert.That(a, Does.Contain(expected));
        Assert.That(Sentences(a), Is.EqualTo(1));
    }
}

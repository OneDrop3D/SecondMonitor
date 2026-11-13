namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Temps / tours restants (barre du haut) et journal des événements de course (bloc Événements de l'accueil).</summary>
[TestFixture]
public class DashboardTests
{
    private static RaceLengthInput TimeRace(double remaining, double leaderDist, double leaderPace, double playerDist, double playerPace) => new()
    {
        IsRace = true,
        LengthType = "Time",
        TimeRemaining = remaining,
        SessionTime = 3600 - remaining,
        LeaderDistance = leaderDist,
        LeaderPace = leaderPace,
        PlayerDistance = playerDist,
        PlayerPace = playerPace,
    };

    [Test]
    public void TimeRace_LeaderFinishesTheLapInProgress_WhenTimeRunsOut()
    {
        // Leader au tour 10,5 ; 10 min restantes à 100 s/tour → 6 tours de plus = 16,5 : il boucle donc 17 tours.
        RaceLengthDto r = RaceLength.Estimate(TimeRace(600, 10.5, 100, 10.5, 100));
        Assert.That(r.Mode, Is.EqualTo("time"));
        Assert.That(r.LeaderTotalLaps, Is.EqualTo(17));
        Assert.That(r.LeaderLapsRemaining, Is.EqualTo(6.5).Within(1e-9));
        Assert.That(r.LeaderLap, Is.EqualTo(11));
        Assert.That(r.Estimated, Is.True);
    }

    [Test]
    public void TimeRace_PlayerJustBehindLeader_DoesTheSameNumberOfLaps()
    {
        RaceLengthDto r = RaceLength.Estimate(TimeRace(600, 10.5, 100, 10.4, 101));
        Assert.That(r.PlayerTotalLaps, Is.EqualTo(17));
        Assert.That(r.PlayerLapsRemaining, Is.EqualTo(6.6).Within(0.01));
        Assert.That(r.PlayerFinishIn, Is.GreaterThan(r.LeaderLapsRemaining!.Value * 100));
    }

    [Test]
    public void TimeRace_PlayerAsLeader_GetsExactlyTheLeaderLaps()
    {
        var input = TimeRace(600, 10.0, 100, 10.0, 100);
        input.PlayerIsLeader = true;
        RaceLengthDto r = RaceLength.Estimate(input);
        // 10 + 6 = 16,0 pile : le leader passe la ligne à 0:00 → 16 tours, pas 17.
        Assert.That(r.LeaderTotalLaps, Is.EqualTo(16));
        Assert.That(r.PlayerTotalLaps, Is.EqualTo(16));
    }

    [Test]
    public void TimeRace_LappedPlayer_DoesOneLapLess()
    {
        // Joueur un tour derrière et plus lent : il reçoit le drapeau à son passage suivant l'arrivée du leader.
        RaceLengthDto r = RaceLength.Estimate(TimeRace(600, 10.5, 100, 9.6, 110));
        Assert.That(r.LeaderTotalLaps, Is.EqualTo(17));
        // Leader arrive dans 650 s ; le joueur aura parcouru 9,6 + 650/110 = 15,5 → 16 tours.
        Assert.That(r.PlayerTotalLaps, Is.EqualTo(16));
        Assert.That(r.PlayerLapsRemaining, Is.EqualTo(6.4).Within(0.01));
    }

    [Test]
    public void TimeWithExtraLap_AddsOneLap()
    {
        var input = TimeRace(600, 10.5, 100, 10.4, 100);
        input.LengthType = "TimeWithExtraLap";
        RaceLengthDto r = RaceLength.Estimate(input);
        Assert.That(r.Mode, Is.EqualTo("time+1"));
        Assert.That(r.LeaderTotalLaps, Is.EqualTo(18));
    }

    [Test]
    public void TimeElapsed_EveryoneFinishesTheCurrentLap()
    {
        RaceLengthDto r = RaceLength.Estimate(TimeRace(0, 16.3, 100, 16.1, 100));
        Assert.That(r.LeaderTotalLaps, Is.EqualTo(17));
        Assert.That(r.PlayerTotalLaps, Is.EqualTo(17));
        Assert.That(r.TimeRemaining, Is.EqualTo(0));

        var finished = TimeRace(0, 17.05, 100, 16.8, 100);
        finished.LeaderFinished = true;
        RaceLengthDto f = RaceLength.Estimate(finished);
        Assert.That(f.LeaderTotalLaps, Is.EqualTo(17));
        Assert.That(f.PlayerTotalLaps, Is.EqualTo(17));
        Assert.That(f.PlayerLapsRemaining, Is.EqualTo(0.2).Within(0.01));
    }

    [Test]
    public void LapsRace_IsExact_AndLappedPlayerLosesLaps()
    {
        var input = new RaceLengthInput { IsRace = true, LengthType = "Laps", TotalLaps = 30, LeaderDistance = 20.5, LeaderPace = 100, PlayerDistance = 19.2, PlayerPace = 104 };
        RaceLengthDto r = RaceLength.Estimate(input);
        Assert.That(r.Mode, Is.EqualTo("laps"));
        Assert.That(r.Estimated, Is.False);
        Assert.That(r.LeaderTotalLaps, Is.EqualTo(30));
        Assert.That(r.LeaderLap, Is.EqualTo(21));
        // Leader arrive dans 950 s, joueur à 19,2 + 950/104 = 28,3 → 29 tours.
        Assert.That(r.PlayerTotalLaps, Is.EqualTo(29));

        input.PlayerPace = null;
        input.LeaderPace = null;
        RaceLengthDto unknown = RaceLength.Estimate(input);
        Assert.That(unknown.PlayerTotalLaps, Is.EqualTo(29), "sans rythme : un tour de retard = un tour de moins");
    }

    [Test]
    public void Practice_GivesLapsPossible_AndTimeProgress()
    {
        var input = new RaceLengthInput { IsRace = false, LengthType = "Time", TimeRemaining = 900, SessionTime = 2700, LeaderDistance = 20, PlayerDistance = 12.3, PlayerPace = 100 };
        RaceLengthDto r = RaceLength.Estimate(input);
        Assert.That(r.PlayerLapsPossible, Is.EqualTo(9));
        Assert.That(r.Progress, Is.EqualTo(0.75).Within(1e-9));
        Assert.That(r.PlayerTotalLaps, Is.Null);
    }

    [Test]
    public void Engine_FillsRaceLength_AndRecordsEvents_InDemoRace()
    {
        string dir = Path.Combine(Path.GetTempPath(), "smhub-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = new DemoRaceSimulator(raceDurationSeconds: 3600);
            var engine = new RaceEngine(dir);
            RaceSnapshot snap = null;
            for (int i = 0; i < 30 * 60 * 10; i++)
            {
                engine.Process(sim.Step(0.1));
                if (i % 10 == 0)
                {
                    snap = engine.BuildSnapshot(); // comme l'interface : un instantané par seconde au moins
                }
            }

            snap = engine.BuildSnapshot();
            Assert.That(snap.RaceLength, Is.Not.Null);
            Assert.That(snap.RaceLength.Mode, Is.EqualTo("time"));
            Assert.That(snap.RaceLength.TimeRemaining, Is.EqualTo(1800).Within(2));
            // Environ 30 min restantes à ~2 min le tour pour les GT3 : une quinzaine de tours restants.
            Assert.That(snap.RaceLength.PlayerLapsRemaining, Is.InRange(12, 18));
            Assert.That(snap.RaceLength.PlayerTotalLaps, Is.LessThanOrEqualTo(snap.RaceLength.LeaderTotalLaps));

            var (session, events) = engine.RaceEvents(0);
            Assert.That(session, Is.GreaterThan(0));
            Assert.That(events, Is.Not.Empty);
            Assert.That(events[0].Kind, Is.EqualTo("session"));
            Assert.That(events.Any(e => e.Kind == "bestlap"), Is.True, "les meilleurs tours sont journalisés");
            Assert.That(events.Any(e => e.Kind == "pit"), Is.True, "les arrêts des voisins sont journalisés");
            Assert.That(snap.LastEventId, Is.EqualTo(events[^1].Id));
            Assert.That(engine.RaceEvents(snap.LastEventId).Events, Is.Empty);
            Assert.That(events.Select(e => e.Id).Distinct().Count(), Is.EqualTo(events.Count));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Test]
    public void EventLog_ReportsFlagOnce_EvenIfItFlickers()
    {
        var log = new RaceEventLog();
        RaceSnapshot Snap(long seq, double t, params string[] flags) => new()
        {
            Sequence = seq,
            HasData = true,
            SessionType = "Race",
            SessionPhase = "Green",
            SessionTime = t,
            Flags = flags.ToList(),
            Drivers = new List<DriverRow> { new() { Id = "a", Name = "A", IsPlayer = true, Position = 1, ClassPosition = 1, ClassId = "c", Laps = 3, CurrentLapValid = true, FinishStatus = "None" } },
        };
        log.Observe(Snap(1, 10), "k");
        log.Observe(Snap(2, 11, "YellowSector2"), "k");
        log.Observe(Snap(3, 11.5), "k");
        log.Observe(Snap(4, 12, "YellowSector2"), "k");
        log.Observe(Snap(5, 13, "YellowSector2"), "k");
        log.Observe(Snap(6, 20), "k");
        log.Observe(Snap(7, 24), "k");
        var flags = log.Since(0).Where(e => e.Kind == "flag").ToList();
        Assert.That(flags.Count(e => e.Title.Contains("jaune secteur 2") && !e.Title.StartsWith("Fin")), Is.EqualTo(1));
        Assert.That(flags.Count(e => e.Title.StartsWith("Fin")), Is.EqualTo(1));

        // Nouvelle session : le journal repart de zéro.
        long before = log.SessionId;
        log.Observe(Snap(8, 1), "autre");
        Assert.That(log.SessionId, Is.GreaterThan(before));
        Assert.That(log.Since(0).Count, Is.EqualTo(1));
    }
}

namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.DataModel.Snapshot;
using SecondMonitor.DataModel.Snapshot.Drivers;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Données inhabituelles des jeux (menus, tour de formation F1, voitures sans nom) : rien ne doit planter.</summary>
[TestFixture]
public class RobustnessTests
{
    [Test]
    public void EmptySession_IsNotShownAsData()
    {
        var engine = new RaceEngine(TempDir());
        engine.Process(new SimulatorDataSet("F1 2024"), SecondMonitor.Remote.Models.DatagramPayloadKind.SessionStart);
        RaceSnapshot snap = engine.BuildSnapshot();
        Assert.That(snap.HasData, Is.False);
        Assert.That(snap.Source, Is.EqualTo("F1 2024"));
    }

    [Test]
    public void FormationLap_NegativeDistances_ZeroLaps_EmptyNames_DoNotThrow()
    {
        var engine = new RaceEngine(TempDir());
        var sim = new DemoRaceSimulator();
        for (int i = 0; i < 600; i++)
        {
            SimulatorDataSet data = sim.Step(0.1);
            foreach (DriverInfo d in data.DriversInfo)
            {
                d.CompletedLaps = 0;
                d.LapDistance = -150 + (i * 0.5); // F1 : avant la ligne pendant le tour de formation
                if (!d.IsPlayer && i % 7 == 0)
                {
                    d.DriverLongName = string.Empty;
                    d.DriverShortName = string.Empty;
                    d.CarName = null;
                    d.CarClassName = null;
                }
            }

            Assert.DoesNotThrow(() => engine.Process(data));
            if (i % 50 == 0)
            {
                RaceSnapshot snap = null;
                Assert.DoesNotThrow(() => snap = engine.BuildSnapshot());
                Assert.DoesNotThrow(() => RaceBriefing.Prepare(snap, "Quel rythme pour gagner ?", new BriefingExtras { Weather = engine.BuildWeather() }));
                Assert.DoesNotThrow(() => engine.BuildMicroSectors());
                Assert.DoesNotThrow(() => engine.BuildSpeedAnalysis());
            }
        }

        Assert.That(engine.BuildSnapshot().HasData, Is.True);
    }

    private static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "smhub-robuste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

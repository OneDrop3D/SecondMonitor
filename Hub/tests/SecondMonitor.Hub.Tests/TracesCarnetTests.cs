namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;

/// <summary>Tracés des tours gardés avec le carnet : relus après la course, supprimés avec lui.</summary>
[TestFixture]
public class TracesCarnetTests
{
    [Test]
    public void Traces_EnregistreesRelues_PuisSupprimeesAvecLeCarnet()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smhub-traces-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CarnetStore(dir);
            var c = new CarnetSession { Id = "s1", Laps = { new CarnetLap { Lap = 1, Time = 90 } } };
            store.Save(c);
            store.SaveLap("s1", new LapDetailDto { Lap = 12, Time = 91.2, Throttle = { 100, null, 40 }, Brake = { 0, 80, null } });
            store.SaveLap("s1", new LapDetailDto { Lap = 3, Time = 92.5 });
            store.SaveLap("../x", new LapDetailDto { Lap = 1 }); // identifiant refusé

            Assert.That(store.TraceLaps("s1"), Is.EqualTo(new[] { 3, 12 }));
            LapDetailDto back = store.LoadLap("s1", 12);
            Assert.That(back.Time, Is.EqualTo(91.2));
            Assert.That(back.Throttle, Is.EqualTo(new double?[] { 100, null, 40 }));
            Assert.That(back.Brake, Is.EqualTo(new double?[] { 0, 80, null }));
            Assert.That(store.LoadLap("s1", 99), Is.Null);
            Assert.That(store.List().Select(s => s.Id), Is.EqualTo(new[] { "s1" }), "le dossier des tracés n'est pas pris pour un carnet");

            Assert.That(store.Delete("s1"), Is.True);
            Assert.That(store.TraceLaps("s1"), Is.Empty);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}

namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

[TestFixture, Explicit]
public class ApercuReponses
{
    [Test]
    public void Afficher()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smhub-apercu-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator();
        var engine = new RaceEngine(dir);
        for (double t = 0; t < 20 * 60; t += 0.1) engine.Process(sim.Step(0.1));
        var snap = engine.BuildSnapshot();
        var extras = new BriefingExtras { Weather = engine.BuildWeather(), MicroSectors = engine.BuildMicroSectors() };
        foreach (string q in new[] {
            "Voilà, j'aurais quoi comme rythme, à peu près ?", "Quel rythme dois-je faire pour gagner la course ?",
            "Comment la météo va-t-elle évoluer durant la course ?", "La piste va évoluer durant toute la course.",
            "quelle est ma position, quelle est la météo, quelles sont les prévisions météo pour la course ?", "Comment vont mes pneus ?" })
        {
            TestContext.Out.WriteLine($"Q : {q}\nR : {RaceBriefing.Prepare(snap, q, extras).Suggestion}\n");
        }
        Directory.Delete(dir, true);
    }
}

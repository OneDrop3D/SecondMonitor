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
        var memory = new RaceMemory();
        var extras = new BriefingExtras { Weather = engine.BuildWeather(), MicroSectors = engine.BuildMicroSectors(), Speeds = engine.BuildSpeedAnalysis(), Laps = engine.PlayerLaps(), Memory = memory };
        foreach (string q in new[] {
            "Mon meilleur tour", "pire tour", "mon dernier tour ?", "ma moyenne ?", "je suis régulier ?", "quelle est ma vmax comparée aux autres",
            "Voilà, j'aurais quoi comme rythme, à peu près ?", "Quel rythme dois-je faire pour gagner la course ?",
            "Comment la météo va-t-elle évoluer durant la course ?", "quelle est ma position, quelle est la météo, quelles sont les prévisions météo pour la course ?",
            "Comment vont mes pneus ?", "Mes pressions sont bonnes ?", "Je rentre quand ?", "Combien de tours d'essence ?", "Écart avec la voiture derrière ?", "batterie ?" })
        {
            BriefingResult r = RaceBriefing.Prepare(snap, q, extras);
            TestContext.Out.WriteLine($"Q : {q}\nR : {r.Suggestion}   [{ExchangeCategorizer.Categorize(q, r.Topics)}]\n");
        }

        // Dialogue : retours du pilote et suites de conversation.
        CarContext car = CarContext.From(snap);
        foreach (string say in new[] {
            "Pilote : j'ai du sous-virage", "c'est en sortie", "autre chose ?",
            "Je bloque au virage 1 à chaque fois la roue intérieure", "non",
            "je bloque les roues avant partout", "oui", "c'est mieux",
            "l'arrière glisse en sortie", "toujours pareil", "la voiture saute sur les vibreurs" })
        {
            DialogReply d = RadioDialog.FollowUp(say, memory, car, 12) ?? RadioDialog.Feedback(say, memory, car, snap, 12);
            TestContext.Out.WriteLine($"Pilote : {say}\nIngénieur : {d?.Text}   [{d?.Kind} · {d?.Category}]\n");
        }

        TestContext.Out.WriteLine("Arrivée : " + RaceFinishRadio.Build(snap));
        Directory.Delete(dir, true);
    }
}

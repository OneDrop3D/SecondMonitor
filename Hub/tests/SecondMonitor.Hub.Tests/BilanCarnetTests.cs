namespace SecondMonitor.Hub.Tests;

using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>Bilan du carnet après la course : valeurs par tour, moyennes par relais et de la course, tours exclus.</summary>
[TestFixture]
public class BilanCarnetTests
{
    private static CarnetLap Tour(int lap, double time, int stint, string gomme, double? fuel, bool pit = false, bool valid = true) =>
        new() { Lap = lap, Time = time, Stint = stint, Compound = gomme, Fuel = fuel, Energy = fuel * 2, Pit = pit, Valid = valid, WearDelta = new[] { 0.4, 0.5, 0.6, 0.7 } };

    /// <summary>Relais 1 en tendres (un accident au tour 2, rentrée au 4), relais 2 en mediums (sortie au 5, un tour invalide hors du rythme au 8).</summary>
    private static CarnetSession Course() => new()
    {
        Id = "20261118-173608-test",
        SessionType = "Race",
        Laps =
        {
            Tour(1, 100, 1, "Soft", 3.0),
            Tour(2, 106, 1, "Soft", 3.6),
            Tour(3, 102, 1, "Soft", 3.0),
            Tour(4, 130, 1, "Soft", 2.0, pit: true),
            Tour(5, 140, 2, "Medium", null, pit: true),
            Tour(6, 99, 2, "Medium", 2.9),
            Tour(7, 99.5, 2, "Medium", 2.9, valid: false),
            Tour(8, 110, 2, "Medium", 3.1, valid: false),
            Tour(9, 100, 2, "Medium", 3.0),
        },
    };

    [Test]
    public void Bilan_MoyennesParRelaisEtCourse_SansLesToursAuStandNiLesInvalidesHorsRythme()
    {
        CarnetBilan b = CarnetBuilder.Bilan(Course());

        Assert.That(b.ExclusDOffice, Is.EqualTo(new[] { 4, 5, 8 }), "stand, et invalide à plus de 2 % ; l'invalide dans le rythme (7) reste compté");
        Assert.That(b.Exclus, Is.EqualTo(b.ExclusDOffice));
        Assert.That(b.Relais.Select(r => (r.Relais, r.Gomme, r.Premier, r.Dernier, r.Comptes)), Is.EqualTo(new[] { (1, "Soft", 1, 4, 3), (2, "Medium", 5, 9, 3) }));
        Assert.That(b.Relais[0].Temps, Is.EqualTo(102.667).Within(0.001));
        Assert.That(b.Relais[0].Essence, Is.EqualTo(3.2).Within(0.001));
        Assert.That(b.Relais[0].Energie, Is.EqualTo(6.4).Within(0.01));
        Assert.That(b.Relais[0].Usure, Is.EqualTo(new[] { 0.4, 0.5, 0.6, 0.7 }));
        Assert.That(b.Relais[1].Temps, Is.EqualTo(99.5).Within(0.001));
        Assert.That(b.Course.Relais, Is.EqualTo(0));
        Assert.That(b.Course.Comptes, Is.EqualTo(6));
        Assert.That(b.Course.Temps, Is.EqualTo((100 + 106 + 102 + 99 + 99.5 + 100) / 6.0).Within(0.001));
        Assert.That(b.Course.Essence, Is.EqualTo((3.0 + 3.6 + 3.0 + 2.9 + 2.9 + 3.0) / 6).Within(0.001));
        Assert.That(b.Tours, Has.Count.EqualTo(9));
    }

    [Test]
    public void CompterTour_RecalculeLesMoyennes_EtSEnregistreAvecLeCarnet()
    {
        CarnetSession c = Course();
        CarnetBuilder.CompterTour(c, 2, compte: false); // accident : usure et essence à part
        CarnetBuilder.CompterTour(c, 8, compte: true); // le pilote juge ce tour invalide représentatif
        CarnetBilan b = CarnetBuilder.Bilan(c);
        Assert.That(b.Exclus, Is.EqualTo(new[] { 2, 4, 5 }));
        Assert.That(b.Relais[0].Temps, Is.EqualTo(101).Within(0.001));
        Assert.That(b.Relais[0].Essence, Is.EqualTo(3.0).Within(0.001));
        Assert.That(b.Relais[1].Comptes, Is.EqualTo(4));

        var dir = Path.Combine(Path.GetTempPath(), "smhub-bilan-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CarnetStore(dir);
            store.Save(c);
            CarnetSession back = store.Load(c.Id);
            Assert.That(back.ToursRetires, Is.EqualTo(new[] { 2 }));
            Assert.That(back.ToursRemis, Is.EqualTo(new[] { 8 }));
            Assert.That(CarnetBuilder.Bilan(back).Exclus, Is.EqualTo(new[] { 2, 4, 5 }));
            Assert.That(back.Laps[0].WearDelta, Is.EqualTo(new[] { 0.4, 0.5, 0.6, 0.7 }));
            Assert.That(back.Laps[0].Compound, Is.EqualTo("Soft"));

            // Revenir au choix d'office n'en garde aucune trace.
            CarnetBuilder.CompterTour(back, 2, compte: true);
            CarnetBuilder.CompterTour(back, 8, compte: false);
            Assert.That(back.ToursRetires, Is.Empty);
            Assert.That(back.ToursRemis, Is.Empty);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Bilan_AncienCarnet_RelaisRetrouvesParLesArrets_EtTempsSeulement()
    {
        var c = new CarnetSession
        {
            Id = "ancien",
            Result = new CarnetResult { PitLaps = { 3 } },
            Tyres = new CarnetTyres { Compound = "Soft" },
        };
        for (int lap = 1; lap <= 6; lap++)
        {
            c.Laps.Add(new CarnetLap { Lap = lap, Time = 200 + lap, Pit = lap is 3 or 4, Valid = true });
        }

        CarnetBilan b = CarnetBuilder.Bilan(c);
        Assert.That(b.Relais.Select(r => (r.Relais, r.Premier, r.Dernier)), Is.EqualTo(new[] { (1, 1, 3), (2, 4, 6) }));
        Assert.That(b.Relais[0].Gomme, Is.Null);
        Assert.That(b.Relais[1].Gomme, Is.EqualTo("Soft"), "la gomme du dernier relais est connue par l'état des pneus");
        Assert.That(b.Relais[1].Temps, Is.EqualTo(205.5).Within(0.001));
        Assert.That(b.Course.Essence, Is.Null);
        Assert.That(b.Course.Usure, Is.Null);
    }

    [Test]
    public void Carnet_EnregistreEssenceUsureGommeEtRelaisDeChaqueTour()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smhub-bilan-" + Guid.NewGuid().ToString("N"));
        var sim = new DemoRaceSimulator(seed: 11);
        var engine = new RaceEngine(dir);
        for (double t = 0; t < 40 * 60; t += 0.1)
        {
            engine.Process(sim.Step(0.1));
        }

        RaceSnapshot snap = engine.BuildSnapshot();
        CarnetSession c = CarnetBuilder.Create(snap, DateTime.Now);
        CarnetBuilder.Update(c, snap, engine.PlayerLaps(), null);
        Directory.Delete(dir, true);

        var enPiste = c.Laps.Where(l => !l.Pit).ToList();
        Assert.That(enPiste, Is.Not.Empty);
        Assert.That(enPiste.All(l => l.Fuel > 0 && l.Energy > 0 && l.WearDelta?.Length == 4 && l.Compound != null && l.Stint >= 1));
        CarnetBilan b = CarnetBuilder.Bilan(c);
        Assert.That(b.Relais, Has.Count.GreaterThanOrEqualTo(2), "au moins un arrêt en 40 minutes de démo");
        Assert.That(b.Relais.All(r => r.Gomme != null));
        Assert.That(b.Course.Essence, Is.GreaterThan(0));
    }
}

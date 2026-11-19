namespace SecondMonitor.Hub.Tests;

using System.Collections.Generic;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Strategy;

/// <summary>Plan préparé avant la course : son arrêt sert tant que le pilote n'a rien décidé à la radio, et seulement sur son circuit.</summary>
[TestFixture]
public class PlanPrepareTests
{
    /// <summary>16 tours bouclés, 18,5 à faire, énergie 95 % (7,9 %/tour), essence 95 L : fenêtre du tour 24 au tour 28.</summary>
    private static RaceSnapshot Snap() => new()
    {
        HasData = true,
        SessionType = "Race",
        Source = "LM Ultimate",
        Track = new TrackDto { Name = "Circuit de la Sarthe" },
        Drivers = new List<DriverRow> { new() { Id = "me", IsPlayer = true, Laps = 16, LapFraction = 0.3, Number = 35, ClassId = "h" } },
        Player = new PlayerStrategy
        {
            LapsRemaining = 18.5,
            Fuel = new FuelStrategy { Remaining = 95, Capacity = 100, PerLap = 7.33, LapsLeft = 95 / 7.33, ToAdd = (19.5 * 7.33) - 95 },
            Energy = new EnergyStrategy { RemainingPct = 95, PerLapPct = 7.9, LapsLeft = 95 / 7.9, ToAddPct = (19.5 * 7.9) - 95 },
        },
    };

    private static RaceMemory Memory(string track) => new()
    {
        Prepared = new PreparedPlan { Track = track, Stops = new() { new PreparedStop { Lap = 10 }, new PreparedStop { Lap = 26, Fuel = 60, Tyres = true } } },
    };

    [Test]
    public void ArretPrepareSertParDefaut()
    {
        RaceMemory memory = Memory("circuit de la sarthe");
        RacePlanDto plan = RacePlanner.Build(Snap(), LiftCoast.Context(memory));
        Assert.That(plan.NextStopLap, Is.EqualTo(26), "le tour 10 est passé : arrêt suivant du plan");
        Assert.That(memory.Facts("Circuit de la Sarthe"), Does.Contain("PLAN PRÉPARÉ AVANT LA COURSE").And.Contain("tour 26 (60,0 L, pneus)"));
    }

    [Test]
    public void DecisionRadioEtAutreCircuitPassentAvant()
    {
        RaceMemory memory = Memory("Circuit de la Sarthe");
        memory.PlannedStopLap = 25;
        Assert.That(RacePlanner.Build(Snap(), LiftCoast.Context(memory)).NextStopLap, Is.EqualTo(25), "la décision du pilote remplace le plan");

        RaceMemory ailleurs = Memory("Spa-Francorchamps");
        Assert.That(RacePlanner.Build(Snap(), LiftCoast.Context(ailleurs)).NextStopLap, Is.EqualTo(RacePlanner.Build(Snap()).NextStopLap), "plan d'un autre circuit ignoré");
        Assert.That(ailleurs.Facts("Circuit de la Sarthe"), Is.Null);
    }
}

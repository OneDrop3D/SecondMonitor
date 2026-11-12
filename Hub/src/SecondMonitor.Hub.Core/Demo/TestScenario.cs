namespace SecondMonitor.Hub.Core.Demo
{
    using System;
    using System.IO;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Situation de course fictive (25 minutes de la course de démo) pour tester l'ingénieur IA sans jeu ni PC Windows.
    /// Calculée une seule fois, à la première demande (environ une seconde).
    /// </summary>
    public static class TestScenario
    {
        private static readonly Lazy<RaceSnapshot> Snapshot = new(Build);

        public static RaceSnapshot Get() => Snapshot.Value;

        private static RaceSnapshot Build()
        {
            string dir = Path.Combine(Path.GetTempPath(), "smhub-test-scenario");
            var engine = new RaceEngine(dir);
            var sim = new DemoRaceSimulator(seed: 42, raceDurationSeconds: 3600);
            const double dt = 0.2;
            for (double t = 0; t < 25 * 60; t += dt)
            {
                engine.Process(sim.Step(dt));
            }

            return engine.BuildSnapshot();
        }
    }
}

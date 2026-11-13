namespace SecondMonitor.Hub.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondMonitor.DataModel.BasicProperties;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Track;

[TestFixture]
public class TrackCalibrationTests
{
    /// <summary>
    /// Simule un jeu : son repère est tourné (et éventuellement en miroir), sa ligne de départ est ailleurs que celle
    /// de la bibliothèque, et sa distance de tour n'est pas exactement proportionnelle à celle du tracé.
    /// </summary>
    private static void Feed(TrackCalibration cal, LibraryTrack t, int gameStart, int gameDir, double angle, bool mirror, double from, double to, int cars = 40, int seed = 1)
    {
        var rnd = new Random(seed);
        int n = t.Points.Count;
        for (int c = 0; c < cars * 10; c++)
        {
            double f = from + ((to - from) * rnd.NextDouble());
            double warped = f + (0.006 * Math.Sin(f * Math.PI * 4)); // écart de mesure du tour
            double s = gameStart + (gameDir * warped * n);
            s -= Math.Floor(s / n) * n;
            int k = (int)s;
            double u = s - k;
            double[] a = t.Points[k % n], b = t.Points[(k + 1) % n];
            double x = a[0] + ((b[0] - a[0]) * u) + (rnd.NextDouble() * 8) - 4;
            double z = (a[1] + ((b[1] - a[1]) * u) + (rnd.NextDouble() * 8) - 4) * (mirror ? -1 : 1);
            cal.Add(f, (x * Math.Cos(angle)) - (z * Math.Sin(angle)) + 1234, (x * Math.Sin(angle)) + (z * Math.Cos(angle)) - 567);
        }
    }

    private static LibraryTrack Track(string name, double length) => TrackLibrary.Find(name, string.Empty, length);

    [TestCase(0, 1, 0.0, false)]
    [TestCase(37, 1, 1.2, true)]
    [TestCase(250, -1, 2.5, false)]
    [TestCase(390, -1, -0.7, true)]
    public void FindsStartAndDirection_FromGamePositions(int gameStart, int gameDir, double angle, bool mirror)
    {
        LibraryTrack t = Track("Circuit de Spa-Francorchamps", 7004);
        var cal = new TrackCalibration();
        Feed(cal, t, gameStart, gameDir, angle, mirror, 0, 1);
        LibraryOutline o = cal.Resolve(t, force: true);

        Assert.That(cal.State, Is.EqualTo(CalibrationState.Calibrated));
        Assert.That(cal.Direction, Is.EqualTo(gameDir));
        int error = Math.Abs(((cal.StartOffset - gameStart) % 400 + 600) % 400 - 200);
        Assert.That(error, Is.LessThanOrEqualTo(3), $"départ trouvé {cal.StartOffset}, attendu {gameStart}");
        Assert.That(o.Calibrated, Is.True);
        Assert.That(o.Points[0], Is.EqualTo(t.Points[cal.StartOffset]));
    }

    [Test]
    public void ACoupleOfCornersAreEnough()
    {
        LibraryTrack t = Track("Silverstone", 5891);
        var cal = new TrackCalibration();
        Feed(cal, t, 120, 1, 0.4, false, 0.05, 0.45, cars: 20);
        cal.Resolve(t, force: true);
        Assert.That(cal.State, Is.EqualTo(CalibrationState.Calibrated));
        Assert.That(cal.Direction, Is.EqualTo(1));
    }

    [Test]
    public void AStraightAloneIsNotEnough_NothingAffirmed()
    {
        LibraryTrack t = Track("Autodromo Nazionale Monza", 5793);
        var cal = new TrackCalibration();
        // Quelques mètres de ligne droite : beaucoup de calages possibles, aucun n'est retenu.
        for (int i = 0; i < 30; i++)
        {
            cal.Add(0.30 + (i * 0.002), 100 + (i * 11.6), 50);
        }

        cal.Resolve(t, force: true);
        Assert.That(cal.State, Is.EqualTo(CalibrationState.Pending));
    }

    [Test]
    public void WrongCircuit_IsRejected()
    {
        LibraryTrack spa = Track("Circuit de Spa-Francorchamps", 7004);
        LibraryTrack monza = Track("Autodromo Nazionale Monza", 5793);
        var cal = new TrackCalibration();
        Feed(cal, monza, 0, 1, 0.3, false, 0, 1);
        Assert.That(cal.Resolve(spa, force: true), Is.Null);
        Assert.That(cal.State, Is.EqualTo(CalibrationState.Rejected));
    }

    [Test]
    public void LowConfidenceTrack_IsHiddenUntilCalibrated()
    {
        LibraryTrack t = Track("Silverstone", 5891);
        var unsure = new LibraryTrack { Id = "x", Name = t.Name, Length = t.Length, Points = t.Points, Confidence = 0.3 };
        var cal = new TrackCalibration();
        Assert.That(cal.Resolve(unsure, force: true), Is.Null);

        Feed(cal, unsure, 77, -1, 0.9, true, 0, 1);
        LibraryOutline o = cal.Resolve(unsure, force: true);
        Assert.That(o, Is.Not.Null);
        Assert.That(o.Calibrated, Is.True);
        Assert.That(cal.Direction, Is.EqualTo(-1));
    }

    [Test]
    public void ReverseLayout_StartsUnsure()
    {
        LibraryTrack t = TrackLibrary.Find("Silverstone Reverse", string.Empty, 5891);
        Assert.That(t, Is.Not.Null);
        Assert.That(t.Confidence, Is.LessThan(LibraryTrack.DisplayConfidence));
        LibraryTrack fwd = Track("Silverstone", 5891);
        Assert.That(t.Points[1], Is.EqualTo(fwd.Points[^1]));
    }

    [Test]
    public void RaceEngine_SendsTheCalibratedOutline()
    {
        // Course de démonstration « déguisée » en Spa : positions monde du jeu = tracé réel tourné, départ du jeu ailleurs.
        string dir = Path.Combine(Path.GetTempPath(), "smhub-cal-" + Guid.NewGuid().ToString("N"));
        try
        {
            LibraryTrack spa = Track("Circuit de Spa-Francorchamps", 7004);
            const int gameStart = 123;
            var sim = new DemoRaceSimulator(raceDurationSeconds: 3600);
            var engine = new RaceEngine(dir);
            for (double t = 0; t < 40; t += 0.2)
            {
                var data = sim.Step(0.2);
                data.SessionInfo.TrackInfo.TrackName = "Circuit de Spa-Francorchamps";
                data.SessionInfo.TrackInfo.TrackLayoutName = string.Empty;
                data.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(7004);
                foreach (var d in data.DriversInfo.Where(d => d != null))
                {
                    double f = (d.LapDistance % 5200) / 5200;
                    d.LapDistance = f * 7004;
                    double[] p = spa.Points[(gameStart + (int)(f * 400)) % 400];
                    d.WorldPosition = new Point3D(Distance.FromMeters(-p[1] + 500), Distance.FromMeters(0), Distance.FromMeters(p[0] - 200));
                }

                engine.Process(data);
            }

            RaceSnapshot snap = engine.BuildSnapshot();
            Assert.That(snap.Track.OutlineSource, Is.EqualTo("bibliothèque"));
            Assert.That(snap.Track.OutlineCalibration, Is.EqualTo("calé"));
            int found = spa.Points.IndexOf(snap.Track.Outline[0]);
            Assert.That(Math.Abs(found - gameStart), Is.LessThanOrEqualTo(3));
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

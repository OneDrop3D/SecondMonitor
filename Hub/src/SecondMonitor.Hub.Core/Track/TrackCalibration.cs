namespace SecondMonitor.Hub.Core.Track
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Recale un tracé de bibliothèque sur le jeu : la ligne de départ et le sens de course des tracés réels
    /// (OpenStreetMap…) ne sont pas toujours sûrs, et un tracé mal calé placerait les voitures au mauvais endroit.
    ///
    /// Chaque voiture en piste donne un couple (fraction du tour, position monde x, z). On garde la dernière position
    /// connue pour 200 tranches du tour, puis on cherche le décalage de départ et le sens pour lesquels une similitude
    /// 2D (rotation, échelle, translation, symétrie éventuelle : le repère du jeu est quelconque) superpose le mieux
    /// le tracé aux positions mesurées. Le calage n'est retenu que s'il se détache nettement de toute autre solution ;
    /// un tracé qui ne colle pas du tout (mauvais circuit) est écarté.
    /// </summary>
    public sealed class TrackCalibration
    {
        public const int Bins = 200;

        private readonly double[] _x = new double[Bins];
        private readonly double[] _z = new double[Bins];
        private readonly bool[] _set = new bool[Bins];
        private int _filled;
        private int _changesSinceFit;
        private long _lastFitTicks;
        private string _trackId;
        private int _coverageAtFit;
        private List<double[]> _oriented;

        public CalibrationState State { get; private set; } = CalibrationState.Pending;

        /// <summary>Indice du tracé de bibliothèque où se trouve la ligne de départ du jeu.</summary>
        public int StartOffset { get; private set; }

        /// <summary>+1 : sens du tracé de bibliothèque ; -1 : sens inverse.</summary>
        public int Direction { get; private set; } = 1;

        /// <summary>Écart moyen (m) du meilleur calage, et du meilleur calage concurrent.</summary>
        public double Rms { get; private set; }

        public double RivalRms { get; private set; }

        public int Coverage => _filled;

        public void Reset()
        {
            Array.Clear(_set);
            _filled = 0;
            _changesSinceFit = 0;
            _coverageAtFit = 0;
            _trackId = null;
            _oriented = null;
            State = CalibrationState.Pending;
            StartOffset = 0;
            Direction = 1;
        }

        /// <summary>Position d'une voiture en piste (fraction du tour depuis la ligne du jeu, x et z du jeu en mètres).</summary>
        public void Add(double lapFraction, double x, double z)
        {
            if (double.IsNaN(lapFraction) || lapFraction < 0 || lapFraction >= 1 || (x == 0 && z == 0) || double.IsNaN(x) || double.IsNaN(z))
            {
                return;
            }

            int bin = Math.Min(Bins - 1, (int)(lapFraction * Bins));
            if (!_set[bin])
            {
                _set[bin] = true;
                _filled++;
                _changesSinceFit += 4;
            }
            else if (Math.Abs(_x[bin] - x) + Math.Abs(_z[bin] - z) > 15)
            {
                _changesSinceFit++;
            }

            _x[bin] = x;
            _z[bin] = z;
        }

        /// <summary>
        /// Tracé à afficher : orienté (départ du jeu à l'indice 0, sens de course) quand il est calé ;
        /// le tracé de la bibliothèque tel quel si sa confiance est suffisante ; sinon null (rien d'affirmé).
        /// </summary>
        public LibraryOutline Resolve(LibraryTrack track, bool force = false)
        {
            if (track == null)
            {
                return null;
            }

            if (track.Id != _trackId)
            {
                _trackId = track.Id;
                _oriented = null;
                _coverageAtFit = 0;
                State = CalibrationState.Pending;
                force = true;
            }

            long nowTicks = Environment.TickCount64;
            bool due = State != CalibrationState.Rejected && _filled >= 20
                       && (force || (_changesSinceFit >= 12 && nowTicks - _lastFitTicks > 1500))
                       && !(State == CalibrationState.Calibrated && _coverageAtFit >= Bins * 0.7 && !force);
            if (due)
            {
                _lastFitTicks = nowTicks;
                _changesSinceFit = 0;
                Fit(track);
            }

            return State switch
            {
                CalibrationState.Calibrated => new LibraryOutline(_oriented ??= Orient(track.Points, StartOffset, Direction), true),
                CalibrationState.Pending when track.Confidence >= LibraryTrack.DisplayConfidence => new LibraryOutline(track.Points, false),
                _ => null,
            };
        }

        private void Fit(LibraryTrack track)
        {
            List<double[]> p = track.Points;
            int n = p.Count;
            if (n < 50)
            {
                return;
            }

            var f = new List<double>(Bins);
            var bx = new List<double>(Bins);
            var bz = new List<double>(Bins);
            for (int i = 0; i < Bins; i++)
            {
                if (_set[i])
                {
                    f.Add((i + 0.5) / Bins);
                    bx.Add(_x[i]);
                    bz.Add(_z[i]);
                }
            }

            int m = f.Count;
            double mx = bx.Average(), mz = bz.Average();
            double b2 = 0;
            for (int i = 0; i < m; i++)
            {
                bx[i] -= mx;
                bz[i] -= mz;
                b2 += (bx[i] * bx[i]) + (bz[i] * bz[i]);
            }

            var results = new List<(double Rms, int Offset, int Dir)>(2 * n);
            var ax = new double[m];
            var az = new double[m];
            for (int dir = 1; dir >= -1; dir -= 2)
            {
                for (int off = 0; off < n; off++)
                {
                    double sx = 0, sz = 0;
                    for (int i = 0; i < m; i++)
                    {
                        double s = off + (dir * f[i] * n);
                        s -= Math.Floor(s / n) * n;
                        int k = (int)s;
                        double t = s - k;
                        double[] a = p[k % n], b = p[(k + 1) % n];
                        ax[i] = a[0] + ((b[0] - a[0]) * t);
                        az[i] = a[1] + ((b[1] - a[1]) * t);
                        sx += ax[i];
                        sz += az[i];
                    }

                    sx /= m;
                    sz /= m;
                    double best = double.MaxValue;
                    for (int mirror = 1; mirror >= -1; mirror -= 2)
                    {
                        double a2 = 0, sxx = 0, sxy = 0;
                        for (int i = 0; i < m; i++)
                        {
                            double x = ax[i] - sx, z = (az[i] - sz) * mirror;
                            a2 += (x * x) + (z * z);
                            sxx += (x * bx[i]) + (z * bz[i]);
                            sxy += (x * bz[i]) - (z * bx[i]);
                        }

                        if (a2 <= 0)
                        {
                            continue;
                        }

                        double k2 = (sxx * sxx) + (sxy * sxy);
                        double scale = Math.Sqrt(k2) / a2;
                        if (scale < 0.8 || scale > 1.25)
                        {
                            continue; // le jeu et la bibliothèque sont en mètres : une autre échelle est un faux calage
                        }

                        best = Math.Min(best, Math.Sqrt(Math.Max(0, b2 - (k2 / a2)) / m));
                    }

                    if (best < double.MaxValue)
                    {
                        results.Add((best, off, dir));
                    }
                }
            }

            if (results.Count == 0)
            {
                return;
            }

            results.Sort((u, v) => u.Rms.CompareTo(v.Rms));
            var top = results[0];
            double rival = results.Skip(1).Where(r => r.Dir != top.Dir || Math.Abs(Wrap(r.Offset - top.Offset, n)) > n * 0.05).Select(r => r.Rms).DefaultIfEmpty(double.MaxValue).First();

            // Étendue des positions mesurées par rapport à la taille du tracé : un bout de ligne droite ne suffit pas.
            double spread = Math.Sqrt(b2 / m);
            double size = Math.Sqrt(p.Average(q => Square(q[0] - p.Average(r => r[0])) + Square(q[1] - p.Average(r => r[1]))));
            double tolerance = Math.Max(20, track.Length * 0.012);

            Rms = top.Rms;
            RivalRms = rival;
            if (_filled >= Bins * 0.7 && top.Rms > Math.Max(60, track.Length * 0.03))
            {
                State = CalibrationState.Rejected; // les positions du jeu ne ressemblent pas à ce tracé
                return;
            }

            bool sure = _filled >= Bins * 0.15 && spread >= size * 0.35 && top.Rms <= tolerance && rival >= Math.Max(2.5 * top.Rms, top.Rms + 30);
            if (sure)
            {
                if (State != CalibrationState.Calibrated || top.Offset != StartOffset || top.Dir != Direction)
                {
                    _oriented = null;
                }

                StartOffset = top.Offset;
                Direction = top.Dir;
                State = CalibrationState.Calibrated;
                _coverageAtFit = _filled;
            }
        }

        private static double Square(double v) => v * v;

        private static int Wrap(int d, int n) => ((d % n) + n + (n / 2)) % n - (n / 2);

        /// <summary>Tracé recommençant à l'indice offset et parcouru dans le sens dir.</summary>
        public static List<double[]> Orient(List<double[]> points, int offset, int dir)
        {
            int n = points.Count;
            var o = new List<double[]>(n);
            for (int k = 0; k < n; k++)
            {
                o.Add(points[(((offset + (dir * k)) % n) + n) % n]);
            }

            return o;
        }
    }

    public enum CalibrationState
    {
        Pending,
        Calibrated,
        Rejected,
    }

    public sealed record LibraryOutline(List<double[]> Points, bool Calibrated);
}

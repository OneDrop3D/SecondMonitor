namespace SecondMonitor.Hub.Core.Demo
{
    using System;
    using System.Linq;

    /// <summary>
    /// Circuit de démonstration « physique » : tracé, courbure, profil de vitesse (adhérence latérale + accélération/freinage)
    /// et ligne de course. Permet à la démo de produire des Vmax/Vmin, trajectoires et températures crédibles.
    /// </summary>
    internal sealed class DemoTrackProfile
    {
        private const double LateralGrip = 28;
        private const double MaxSpeed = 88;
        private const double Acceleration = 8;
        private const double Braking = 22;

        /// <summary>Points de passage du circuit (m) : ligne droite des stands, épingle, enchaînements, grande courbe, chicane.</summary>
        private static readonly (double X, double Z)[] ControlPoints =
        {
            (0, 0), (450, -10), (900, 0), (1040, 40), (1085, 150), (1000, 225), (820, 235), (700, 300), (660, 420), (730, 520),
            (860, 560), (1000, 525), (1150, 560), (1300, 650), (1350, 780), (1250, 885), (1050, 905), (500, 900), (300, 880), (250, 800),
            (320, 740), (270, 670), (150, 620), (0, 600), (-150, 520), (-200, 380), (-150, 200), (-110, 70),
        };

        private double[] _x;
        private double[] _z;
        private double[] _nx;
        private double[] _nz;
        private double[] _curvature;
        private double[] _speed;
        private double _step;
        private double _maxProfileSpeed;

        public double Length { get; private set; }

        /// <summary>Temps au tour du profil brut (s) ; la vitesse de chaque voiture est mise à l'échelle de son rythme.</summary>
        public double LapTime { get; private set; }

        public static DemoTrackProfile Build(double length, int points)
        {
            var p = new DemoTrackProfile { Length = length, _step = length / points };
            int fine = points * 8;
            var fx = new double[fine];
            var fz = new double[fine];
            int cp = ControlPoints.Length;
            for (int i = 0; i < fine; i++)
            {
                double u = (double)i / fine * cp;
                int k = (int)Math.Floor(u);
                double t = u - k;
                var (p0, p1, p2, p3) = (ControlPoints[(k - 1 + cp) % cp], ControlPoints[k % cp], ControlPoints[(k + 1) % cp], ControlPoints[(k + 2) % cp]);
                fx[i] = CatmullRom(p0.X, p1.X, p2.X, p3.X, t);
                fz[i] = CatmullRom(p0.Z, p1.Z, p2.Z, p3.Z, t);
            }

            // Ré-échantillonnage à pas constant le long de la courbe, à l'échelle de la longueur annoncée.
            var cum = new double[fine + 1];
            for (int i = 1; i <= fine; i++)
            {
                int a = i - 1;
                int b = i % fine;
                cum[i] = cum[i - 1] + Math.Sqrt(Math.Pow(fx[b] - fx[a], 2) + Math.Pow(fz[b] - fz[a], 2));
            }

            double scale = length / cum[fine];
            p._x = new double[points];
            p._z = new double[points];
            int j = 0;
            for (int k = 0; k < points; k++)
            {
                double target = k * cum[fine] / points;
                while (j < fine - 1 && cum[j + 1] < target)
                {
                    j++;
                }

                double f = (target - cum[j]) / Math.Max(1e-9, cum[j + 1] - cum[j]);
                int b = (j + 1) % fine;
                p._x[k] = (fx[j] + ((fx[b] - fx[j]) * f)) * scale;
                p._z[k] = (fz[j] + ((fz[b] - fz[j]) * f)) * scale;
            }

            int n = points;
            p._nx = new double[n];
            p._nz = new double[n];
            var heading = new double[n];
            for (int i = 0; i < n; i++)
            {
                int b = (i + 1) % n;
                int a = (i - 1 + n) % n;
                double dx = p._x[b] - p._x[a];
                double dz = p._z[b] - p._z[a];
                double len = Math.Max(1e-9, Math.Sqrt((dx * dx) + (dz * dz)));
                p._nx[i] = -dz / len;
                p._nz[i] = dx / len;
                heading[i] = Math.Atan2(dz, dx);
            }

            var raw = new double[n];
            for (int i = 0; i < n; i++)
            {
                double d = heading[(i + 1) % n] - heading[(i - 1 + n) % n];
                d = Math.Atan2(Math.Sin(d), Math.Cos(d));
                raw[i] = d / (2 * p._step);
            }

            p._curvature = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int k = -4; k <= 4; k++)
                {
                    sum += raw[(i + k + n) % n];
                }

                p._curvature[i] = sum / 9;
            }

            // Profil de vitesse : limite d'adhérence, puis passes accélération / freinage (deux tours pour boucler).
            var v = p._curvature.Select(c => Math.Min(MaxSpeed, Math.Sqrt(LateralGrip / Math.Max(1e-5, Math.Abs(c))))).ToArray();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int k = 1; k <= n; k++)
                {
                    int i = k % n;
                    int prev = (i - 1 + n) % n;
                    double acc = Acceleration * Math.Max(0.15, 1 - (v[prev] / 100));
                    v[i] = Math.Min(v[i], Math.Sqrt((v[prev] * v[prev]) + (2 * acc * p._step)));
                }

                for (int k = n - 1; k >= -1; k--)
                {
                    int i = (k + n) % n;
                    int next = (i + 1) % n;
                    v[i] = Math.Min(v[i], Math.Sqrt((v[next] * v[next]) + (2 * Braking * p._step)));
                }
            }

            p._speed = v;
            p._maxProfileSpeed = v.Max();
            p.LapTime = v.Sum(s => p._step / s);
            return p;
        }

        public double SpeedAt(double s) => Interp(_speed, s);

        private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;
            return 0.5 * ((2 * p1) + ((-p0 + p2) * t) + (((2 * p0) - (5 * p1) + (4 * p2) - p3) * t2) + ((-p0 + (3 * p1) - (3 * p2) + p3) * t3));
        }

        public double CurvatureAt(double s) => Interp(_curvature, s);

        /// <summary>0 en pleine ligne droite, proche de 1 au point le plus lent.</summary>
        public double Cornerness(double s) => 1 - (SpeedAt(s) / _maxProfileSpeed);

        /// <summary>Décalage latéral (m, positif vers l'intérieur d'un virage à gauche) : extérieur - corde - extérieur, plus le style du pilote.</summary>
        public double RacingLineOffset(double s, double bias)
        {
            double k = CurvatureAt(s);
            double entry = CurvatureAt(s + 70);
            double exit = CurvatureAt(s - 70);
            double o = (5 * Math.Tanh(250 * k)) - (3.5 * Math.Tanh(250 * entry)) - (3.5 * Math.Tanh(250 * exit));
            o += bias * (0.4 + Cornerness(s));
            return Math.Clamp(o, -6.5, 6.5);
        }

        public (double X, double Z) Position(double s, double offset)
        {
            double x = Interp(_x, s);
            double z = Interp(_z, s);
            double nx = Interp(_nx, s);
            double nz = Interp(_nz, s);
            return (x + (nx * offset), z + (nz * offset));
        }

        private double Interp(double[] a, double s)
        {
            int n = a.Length;
            double pos = ((s % Length) + Length) % Length / _step;
            int i = (int)Math.Floor(pos) % n;
            double f = pos - Math.Floor(pos);
            return a[i] + ((a[(i + 1) % n] - a[i]) * f);
        }
    }

    internal sealed class DemoThermal
    {
        public double[] Surface { get; } = { 55, 55, 55, 55 };

        public double[] Core { get; } = { 50, 50, 50, 50 };

        public double[] Brake { get; } = { 150, 150, 150, 150 };

        public double Water { get; set; } = 70;

        public double Oil { get; set; } = 75;
    }
}

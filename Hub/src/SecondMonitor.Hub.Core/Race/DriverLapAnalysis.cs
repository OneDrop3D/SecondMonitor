namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Analyse fine d'un pilote, calculée uniquement à partir de (temps, distance, vitesse, position) — donc identique pour tous les jeux :
    /// - micro-secteurs : le tour est coupé en <see cref="MicroSectorCount"/> tronçons égaux, chronométrés par interpolation ;
    /// - trace du tour : vitesse (et position x/z) tous les ~10 m, pour les Vmax/Vmin et les trajectoires.
    /// La précision dépend de la fréquence d'envoi du jeu (voir <see cref="SampleHz"/>).
    /// </summary>
    public sealed class DriverLapAnalysis
    {
        public const int MicroSectorCount = 24;
        public const double TargetBinMeters = 10;
        public const int MaxStoredLaps = 250;

        private readonly LapProgress _progress = new();
        private readonly Dictionary<int, double> _lineTimes = new();
        private double _prevSpeed;
        private double? _prevX;
        private double? _prevZ;
        private long _lastBoundary = -1;
        private double _lastBoundaryTime;
        private double _avgDt;
        private LapTrace _current;
        private bool _gapInLap;

        public double LapLength { get; private set; }

        public int BinCount { get; private set; }

        public double BinSize => BinCount == 0 ? 0 : LapLength / BinCount;

        /// <summary>Fréquence moyenne des données reçues pour ce pilote (Hz).</summary>
        public double SampleHz => _avgDt > 0 ? 1.0 / _avgDt : 0;

        public double?[] CurrentMs { get; private set; } = new double?[MicroSectorCount];

        public bool[] CurrentMsPit { get; private set; } = new bool[MicroSectorCount];

        public double?[] LastMs { get; private set; } = new double?[MicroSectorCount];

        public bool[] LastMsPit { get; private set; } = new bool[MicroSectorCount];

        public double?[] BestMs { get; } = new double?[MicroSectorCount];

        /// <summary>Micro-secteurs du meilleur tour complet (hors stands) : référence du delta en direct.</summary>
        public double?[] BestLapMs { get; private set; }

        private double _bestLapMsSum = double.MaxValue;

        /// <summary>Index du micro-secteur en cours (0..N-1), -1 si inconnu.</summary>
        public int CurrentMsIndex { get; private set; } = -1;

        /// <summary>Temps du tour en cours au dernier micro-secteur franchi (pour le delta en direct).</summary>
        public double? CurrentLapElapsedAtBoundary { get; private set; }

        public List<LapTrace> Traces { get; } = new();

        public LapTrace LastTrace => Traces.LastOrDefault(t => t.Complete);

        public LapTrace BestTrace { get; private set; }

        public void Update(double now, int completedLaps, double lapDistance, double lapLength, double speedKph, double? x, double? z, bool inPits)
        {
            if (lapLength <= 100)
            {
                return;
            }

            if (Math.Abs(lapLength - LapLength) > 1)
            {
                LapLength = lapLength;
                BinCount = Math.Max(20, (int)Math.Round(lapLength / TargetBinMeters));
                ResetLapState();
            }

            bool continuous = _progress.Push(now, completedLaps, lapDistance, lapLength, out double u1, out bool rebased);
            if (rebased)
            {
                _lastBoundary = -1;
                _gapInLap = true;
                Remember(now, u1, speedKph, x, z);
                return;
            }

            if (!continuous)
            {
                return;
            }

            double t0 = _progress.PreviousTime;
            double u0 = _progress.PreviousUnwrapped;
            double dt = now - t0;
            _avgDt = _avgDt <= 0 ? dt : (_avgDt * 0.9) + (dt * 0.1);
            if (dt > 5)
            {
                _gapInLap = true;
                _lastBoundary = -1;
            }

            if (u1 > u0)
            {
                CrossMicroSectors(t0, u0, now, u1, inPits);
                FillBins(u0, u1, speedKph, x, z, inPits);
            }

            Remember(now, u1, speedKph, x, z);
        }

        private void Remember(double now, double u, double speed, double? x, double? z)
        {
            _progress.Commit(now, u);
            _prevSpeed = speed;
            _prevX = x;
            _prevZ = z;
        }

        private void ResetLapState()
        {
            _lastBoundary = -1;
            _current = null;
            Traces.Clear();
            BestTrace = null;
            _lineTimes.Clear();
            Array.Clear(CurrentMs);
            Array.Clear(LastMs);
            Array.Clear(BestMs);
        }

        private void CrossMicroSectors(double t0, double u0, double t1, double u1, bool inPits)
        {
            double msLen = LapLength / MicroSectorCount;
            long first = (long)Math.Floor(u0 / msLen) + 1;
            long last = (long)Math.Floor(u1 / msLen);
            for (long g = first; g <= last; g++)
            {
                double ub = g * msLen;
                double t = t0 + ((ub - u0) / (u1 - u0) * (t1 - t0));
                int seg = (int)((g - 1) % MicroSectorCount);
                if (_lastBoundary == g - 1)
                {
                    double time = t - _lastBoundaryTime;
                    if (time > 0 && time < 90)
                    {
                        CurrentMs[seg] = time;
                        CurrentMsPit[seg] = inPits;
                    }
                }

                _lastBoundary = g;
                _lastBoundaryTime = t;
                CurrentMsIndex = (int)(g % MicroSectorCount);

                if (g % MicroSectorCount == 0)
                {
                    int lapStarting = (int)(g / MicroSectorCount);
                    _lineTimes[lapStarting] = t;
                    _lineTimes.Remove(lapStarting - 3);
                    CompleteMicroSectorLap();
                    CurrentLapElapsedAtBoundary = 0;
                }
                else if (_lineTimes.TryGetValue((int)(g / MicroSectorCount), out double start))
                {
                    CurrentLapElapsedAtBoundary = t - start;
                }
                else
                {
                    CurrentLapElapsedAtBoundary = null;
                }
            }
        }

        private void CompleteMicroSectorLap()
        {
            LastMs = CurrentMs;
            LastMsPit = CurrentMsPit;
            for (int i = 0; i < MicroSectorCount; i++)
            {
                if (LastMs[i] is double v && !LastMsPit[i] && (BestMs[i] == null || v < BestMs[i]))
                {
                    BestMs[i] = v;
                }
            }

            if (LastMs.All(v => v.HasValue) && !LastMsPit.Any(p => p))
            {
                double sum = LastMs.Sum(v => v.Value);
                if (sum < _bestLapMsSum)
                {
                    _bestLapMsSum = sum;
                    BestLapMs = LastMs;
                }
            }

            CurrentMs = new double?[MicroSectorCount];
            CurrentMsPit = new bool[MicroSectorCount];
        }

        /// <summary>
        /// Delta en direct (s, négatif = plus rapide) au dernier micro-secteur franchi : par rapport au meilleur tour et au tour précédent.
        /// Mis à jour 24 fois par tour, comme les barres « Δ » de SecondMonitor.
        /// </summary>
        public (double? VsBest, double? VsLast) LiveDelta()
        {
            int k = CurrentMsIndex;
            if (k <= 0)
            {
                return (null, null);
            }

            return (Delta(BestLapMs, k), Delta(LastMs.All(v => v.HasValue) && !LastMsPit.Any(p => p) ? LastMs : null, k));
        }

        private double? Delta(double?[] reference, int k)
        {
            if (reference == null)
            {
                return null;
            }

            double d = 0;
            for (int i = 0; i < k; i++)
            {
                if (CurrentMs[i] is not double c || reference[i] is not double r || CurrentMsPit[i])
                {
                    return null;
                }

                d += c - r;
            }

            return Math.Round(d, 3);
        }

        private void FillBins(double u0, double u1, double v1, double? x1, double? z1, bool inPits)
        {
            double size = BinSize;
            long first = (long)Math.Floor((u0 / size) - 0.5) + 1;
            long last = (long)Math.Floor((u1 / size) - 0.5);
            for (long g = first; g <= last; g++)
            {
                double center = (g + 0.5) * size;
                double f = (center - u0) / (u1 - u0);
                int lap = (int)(g / BinCount);
                int bin = (int)(g % BinCount);
                if (_current == null || _current.Lap != lap)
                {
                    StartTrace(lap);
                }

                _current.Speed[bin] = (float)(_prevSpeed + ((v1 - _prevSpeed) * f));
                if (_prevX.HasValue && _prevZ.HasValue && x1.HasValue && z1.HasValue)
                {
                    _current.X[bin] = (float)(_prevX.Value + ((x1.Value - _prevX.Value) * f));
                    _current.Z[bin] = (float)(_prevZ.Value + ((z1.Value - _prevZ.Value) * f));
                }

                _current.Filled++;
            }

            if (_current != null)
            {
                _current.Pit |= inPits;
            }
        }

        private void StartTrace(int lap)
        {
            if (_current != null)
            {
                FinishTrace(_current);
            }

            _current = new LapTrace(lap, BinCount);
            _gapInLap = false;
        }

        private void FinishTrace(LapTrace trace)
        {
            trace.Complete = !_gapInLap && trace.Filled >= BinCount * 0.9;
            if (_lineTimes.TryGetValue(trace.Lap, out double start) && _lineTimes.TryGetValue(trace.Lap + 1, out double end))
            {
                trace.Time = end - start;
            }

            if (!trace.Complete)
            {
                return;
            }

            Traces.Add(trace);
            bool clean = !trace.Pit && trace.Time.HasValue;
            if (clean && (BestTrace == null || trace.Time < BestTrace.Time))
            {
                BestTrace?.DropPositions();

                BestTrace = trace;
            }

            // On ne garde les positions (trajectoires) que du dernier et du meilleur tour.
            if (Traces.Count >= 2)
            {
                LapTrace previous = Traces[^2];
                if (previous != BestTrace)
                {
                    previous.DropPositions();
                }
            }

            if (Traces.Count > MaxStoredLaps)
            {
                Traces.RemoveAt(0);
            }
        }
    }

    /// <summary>Vitesse (km/h) et position par tranche de ~10 m sur un tour.</summary>
    public sealed class LapTrace
    {
        public LapTrace(int lap, int bins)
        {
            Lap = lap;
            Speed = Fill(new float[bins]);
            X = Fill(new float[bins]);
            Z = Fill(new float[bins]);
        }

        /// <summary>Numéro du tour (0 = premier tour après le départ).</summary>
        public int Lap { get; }

        public float[] Speed { get; }

        public float[] X { get; private set; }

        public float[] Z { get; private set; }

        public int Filled { get; set; }

        public bool Pit { get; set; }

        public bool Complete { get; set; }

        /// <summary>Temps du tour mesuré par le hub (franchissements de ligne interpolés).</summary>
        public double? Time { get; set; }

        public bool HasPositions => X != null && X.Any(v => !float.IsNaN(v));

        /// <summary>Cache des valeurs par segment de piste (Vmax/Vmin), invalidé quand le découpage change.</summary>
        internal int SegmentsVersion { get; set; } = -1;

        internal double?[] SegmentValues { get; set; }

        public void DropPositions()
        {
            X = null;
            Z = null;
        }

        private static float[] Fill(float[] a)
        {
            Array.Fill(a, float.NaN);
            return a;
        }
    }
}

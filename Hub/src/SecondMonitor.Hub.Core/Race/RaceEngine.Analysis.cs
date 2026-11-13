namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Strategy;
    using SecondMonitor.Hub.Core.Telemetry;

    /// <summary>Analyses détaillées servies à la demande (pas dans le flux 5 Hz) : vitesses, trajectoires, micro-secteurs, tours, télémétrie, météo.</summary>
    public sealed partial class RaceEngine
    {
        private List<TrackSegment> _segments = new();
        private int _segmentsVersion;
        private string _segmentsReference;

        public TelemetryHistoryDto ReadTelemetry(double since)
        {
            lock (_lock)
            {
                return _telemetry.Read(since);
            }
        }

        public WeatherReportDto BuildWeather()
        {
            lock (_lock)
            {
                return _weather.Build(_lastSessionTime);
            }
        }

        public List<LapDto> PlayerLaps()
        {
            lock (_lock)
            {
                return _player.Laps();
            }
        }

        /// <summary>Tours de tous les pilotes (format compact) + régression du relais en cours pour la projection.</summary>
        public List<DriverLapsDto> AllLaps()
        {
            lock (_lock)
            {
                var list = new List<DriverLapsDto>();
                foreach (DriverTracker t in Ordered())
                {
                    var dto = new DriverLapsDto
                    {
                        Id = t.Id,
                        Laps = t.Laps.Select(l => new object[] { l.Lap, l.Time.HasValue ? Math.Round(l.Time.Value, 3) : null, (l.IsPitLap ? 1 : 0) | (l.IsOutLap ? 2 : 0) | (l.Valid ? 0 : 4) }).ToList(),
                    };
                    if (t.CurrentStintFit() is { } fit && t.CurrentStint != null)
                    {
                        dto.Fit = new StintFitDto
                        {
                            Slope = Math.Round(fit.Slope, 4),
                            Intercept = Math.Round(fit.Intercept, 3),
                            FromLap = t.CurrentStint.StartLap + 1,
                            Laps = fit.Count,
                        };
                    }

                    list.Add(dto);
                }

                return list;
            }
        }

        public MicroSectorReportDto BuildMicroSectors()
        {
            lock (_lock)
            {
                var report = new MicroSectorReportDto { Count = DriverLapAnalysis.MicroSectorCount };
                var byClass = new Dictionary<string, double?[]>();
                foreach (DriverTracker t in Ordered())
                {
                    DriverLapAnalysis a = t.Analysis;
                    report.LapLength = Math.Max(report.LapLength, a.LapLength);
                    var dto = new DriverMicroSectorsDto
                    {
                        Id = t.Id,
                        CurrentIndex = a.CurrentMsIndex,
                        Current = Round(a.CurrentMs),
                        Last = Round(a.LastMs),
                        LastPit = a.LastMsPit.ToArray(),
                        Best = Round(a.BestMs),
                        SampleHz = Math.Round(a.SampleHz, 1),
                    };
                    if (a.BestMs.All(v => v.HasValue))
                    {
                        dto.TheoreticalBest = Math.Round(a.BestMs.Sum(v => v.Value), 3);
                    }

                    report.Drivers.Add(dto);
                    string cls = t.Latest?.CarClassId ?? string.Empty;
                    if (!byClass.TryGetValue(cls, out double?[] best))
                    {
                        byClass[cls] = best = new double?[report.Count];
                    }

                    for (int i = 0; i < report.Count; i++)
                    {
                        if (a.BestMs[i] is double v && (best[i] == null || v < best[i]))
                        {
                            best[i] = Math.Round(v, 3);
                        }
                    }
                }

                report.ClassBest = byClass;
                return report;
            }
        }

        public SpeedAnalysisDto BuildSpeedAnalysis()
        {
            lock (_lock)
            {
                EnsureSegments();
                var dto = new SpeedAnalysisDto { Segments = _segments, Reference = _segmentsReference };
                if (_segments.Count == 0)
                {
                    dto.Message = "Découpage du circuit en cours : il faut un tour complet chronométré (sans passage au stand).";
                }

                foreach (DriverTracker t in Ordered())
                {
                    DriverLapAnalysis a = t.Analysis;
                    var row = new DriverSpeedDto { Id = t.Id, SampleHz = Math.Round(a.SampleHz, 1) };
                    var traces = a.Traces.Where(tr => tr.Complete).ToList();
                    row.LapsAnalysed = traces.Count;
                    LapTrace last = traces.LastOrDefault();
                    if (last != null)
                    {
                        row.LastLap = last.Lap + 1;
                        row.TopSpeedLast = Max(last.Speed);
                    }

                    var clean = traces.Where(tr => !tr.Pit).ToList();
                    row.TopSpeedBest = clean.Select(tr => Max(tr.Speed)).Where(v => v.HasValue).DefaultIfEmpty(null).Max();
                    // Tour de la meilleure Vmax et moyenne des Vmax par tour (réponse radio « vitesse max au tour X, en moyenne … »).
                    var tops = clean.Select(tr => (tr.Lap, Top: Max(tr.Speed))).Where(x => x.Top.HasValue).ToList();
                    row.TopSpeedBestLap = tops.Count > 0 ? tops.OrderByDescending(x => x.Top).First().Lap + 1 : null;
                    row.TopSpeedAverage = tops.Count > 0 ? Math.Round(tops.Average(x => x.Top.Value), 1) : null;
                    if (_segments.Count > 0)
                    {
                        double?[] lastValues = last != null ? SegmentValues(last) : null;
                        for (int i = 0; i < _segments.Count; i++)
                        {
                            TrackSegment seg = _segments[i];
                            if (lastValues?[i] is double lv)
                            {
                                row.Last[seg.Id] = lv;
                            }

                            // Meilleure valeur de la session : la plus haute (Vmax en ligne droite comme vitesse de passage en virage).
                            double? best = clean.Select(tr => SegmentValues(tr)[i]).Where(v => v.HasValue).DefaultIfEmpty(null).Max();
                            if (best.HasValue)
                            {
                                row.Best[seg.Id] = best.Value;
                            }
                        }
                    }

                    dto.Drivers.Add(row);
                }

                return dto;
            }
        }

        /// <summary>Profils de vitesse (km/h par tranche de ~10 m) du dernier ou du meilleur tour des pilotes demandés.</summary>
        public SpeedTracesDto BuildSpeedTraces(IEnumerable<string> ids, bool best)
        {
            lock (_lock)
            {
                EnsureSegments();
                var dto = new SpeedTracesDto { Segments = _segments };
                foreach (string id in ids.Distinct().Take(8))
                {
                    if (!_trackers.TryGetValue(id, out DriverTracker t))
                    {
                        continue;
                    }

                    LapTrace trace = best ? t.Analysis.BestTrace ?? t.Analysis.LastTrace : t.Analysis.LastTrace;
                    if (trace == null)
                    {
                        continue;
                    }

                    dto.BinSize = t.Analysis.BinSize;
                    dto.Drivers.Add(new SpeedTraceDto
                    {
                        Id = id,
                        Lap = trace.Lap + 1,
                        Time = trace.Time.HasValue ? Math.Round(trace.Time.Value, 3) : null,
                        Speed = trace.Speed.Select(v => float.IsNaN(v) ? (double?)null : Math.Round(v, 1)).ToList(),
                    });
                }

                return dto;
            }
        }

        /// <summary>
        /// Trajectoires (x, z, vitesse) sur une zone (virage ou ligne droite, avec ~60 m de marge) ou sur tout le tour.
        /// Seuls le dernier et le meilleur tour de chaque pilote gardent leurs positions.
        /// </summary>
        public TrajectoriesDto BuildTrajectories(IEnumerable<string> ids, string segmentId, bool best)
        {
            lock (_lock)
            {
                EnsureSegments();
                var dto = new TrajectoriesDto();
                TrackSegment seg = _segments.FirstOrDefault(s => s.Id == segmentId);
                dto.Segment = seg;
                foreach (string id in ids.Distinct().Take(8))
                {
                    if (!_trackers.TryGetValue(id, out DriverTracker t))
                    {
                        continue;
                    }

                    LapTrace trace = best ? t.Analysis.BestTrace : t.Analysis.LastTrace;
                    if (trace == null || !trace.HasPositions)
                    {
                        trace = t.Analysis.Traces.LastOrDefault(tr => tr.HasPositions);
                    }

                    if (trace == null)
                    {
                        continue;
                    }

                    int n = trace.Speed.Length;
                    IEnumerable<int> bins = seg == null ? Enumerable.Range(0, n).Where(i => i % 2 == 0) : Window(seg, n, 6);
                    var points = new List<double[]>();
                    foreach (int i in bins)
                    {
                        if (!float.IsNaN(trace.X[i]) && !float.IsNaN(trace.Z[i]))
                        {
                            points.Add(new[] { Math.Round(trace.X[i], 2), Math.Round(trace.Z[i], 2), float.IsNaN(trace.Speed[i]) ? 0 : Math.Round(trace.Speed[i], 1) });
                        }
                    }

                    dto.Drivers.Add(new TrajectoryDto
                    {
                        Id = id,
                        Lap = trace.Lap + 1,
                        Time = trace.Time.HasValue ? Math.Round(trace.Time.Value, 3) : null,
                        SampleHz = Math.Round(t.Analysis.SampleHz, 1),
                        Points = points,
                    });
                }

                return dto;
            }
        }

        private static IEnumerable<int> Window(TrackSegment seg, int n, int margin)
        {
            int from = (seg.FromBin - margin + n) % n;
            int to = (seg.ToBin + margin) % n;
            int i = from;
            for (int guard = 0; guard <= n; guard++)
            {
                yield return i;
                if (i == to)
                {
                    yield break;
                }

                i = (i + 1) % n;
            }
        }

        private IEnumerable<DriverTracker> Ordered() => _trackers.Values
            .Where(t => t.Latest != null)
            .OrderBy(t => t.Latest.Position <= 0 ? int.MaxValue : t.Latest.Position);

        /// <summary>Découpe le circuit une fois par session, d'après le meilleur tour disponible (joueur en priorité, mieux échantillonné).</summary>
        private void EnsureSegments()
        {
            if (_segments.Count > 0)
            {
                return;
            }

            var candidates = _trackers.Values
                .Select(t => (Tracker: t, Trace: t.Analysis.BestTrace ?? t.Analysis.Traces.LastOrDefault(tr => tr.Complete && !tr.Pit)))
                .Where(c => c.Trace != null)
                .OrderByDescending(c => c.Tracker.Latest?.IsPlayer == true)
                .ThenByDescending(c => c.Tracker.Analysis.SampleHz)
                .ToList();
            foreach (var (tracker, trace) in candidates.Take(3))
            {
                List<TrackSegment> segments = TrackSegmenter.Detect(trace.Speed, tracker.Analysis.LapLength);
                if (segments.Count >= 2)
                {
                    _segments = segments;
                    _segmentsVersion++;
                    _segmentsReference = tracker.Latest?.DriverLongName;
                    return;
                }
            }
        }

        private double?[] SegmentValues(LapTrace trace)
        {
            if (trace.SegmentsVersion != _segmentsVersion || trace.SegmentValues == null)
            {
                trace.SegmentValues = trace.Speed.Length == 0 ? Array.Empty<double?>() : _segments.Select(s => TrackSegmenter.Measure(trace.Speed, s)).ToArray();
                trace.SegmentsVersion = _segmentsVersion;
            }

            return trace.SegmentValues;
        }

        private static double? Max(float[] speeds)
        {
            var valid = speeds.Where(v => !float.IsNaN(v)).ToList();
            return valid.Count == 0 ? null : Math.Round(valid.Max(), 1);
        }

        private static double?[] Round(double?[] values) => values.Select(v => v.HasValue ? Math.Round(v.Value, 3) : (double?)null).ToArray();
    }

    public sealed class DriverLapsDto
    {
        public string Id { get; set; }

        /// <summary>[tour, temps (s) ou null, drapeaux : 1 = stand, 2 = tour de sortie, 4 = invalide].</summary>
        public List<object[]> Laps { get; set; } = new();

        public StintFitDto Fit { get; set; }
    }

    public sealed class StintFitDto
    {
        /// <summary>temps(tour) = Slope × tour + Intercept, sur le relais en cours.</summary>
        public double Slope { get; set; }

        public double Intercept { get; set; }

        public int FromLap { get; set; }

        public int Laps { get; set; }
    }

    public sealed class MicroSectorReportDto
    {
        public int Count { get; set; }

        public double LapLength { get; set; }

        public List<DriverMicroSectorsDto> Drivers { get; set; } = new();

        /// <summary>Meilleur temps de chaque micro-secteur, par catégorie.</summary>
        public Dictionary<string, double?[]> ClassBest { get; set; } = new();
    }

    public sealed class DriverMicroSectorsDto
    {
        public string Id { get; set; }

        public int CurrentIndex { get; set; }

        public double?[] Current { get; set; }

        public double?[] Last { get; set; }

        public bool[] LastPit { get; set; }

        public double?[] Best { get; set; }

        /// <summary>Somme des meilleurs micro-secteurs (meilleur tour « théorique »).</summary>
        public double? TheoreticalBest { get; set; }

        public double SampleHz { get; set; }
    }

    public sealed class SpeedAnalysisDto
    {
        public List<TrackSegment> Segments { get; set; } = new();

        public string Reference { get; set; }

        public string Message { get; set; }

        public List<DriverSpeedDto> Drivers { get; set; } = new();
    }

    public sealed class DriverSpeedDto
    {
        public string Id { get; set; }

        public double SampleHz { get; set; }

        public int LapsAnalysed { get; set; }

        public int? LastLap { get; set; }

        public double? TopSpeedLast { get; set; }

        public double? TopSpeedBest { get; set; }

        public int? TopSpeedBestLap { get; set; }

        public double? TopSpeedAverage { get; set; }

        /// <summary>Par segment : Vmin (virage) ou Vmax (ligne droite), en km/h, au dernier tour complet.</summary>
        public Dictionary<string, double> Last { get; set; } = new();

        /// <summary>Meilleure valeur de la session par segment.</summary>
        public Dictionary<string, double> Best { get; set; } = new();
    }

    public sealed class SpeedTracesDto
    {
        public double BinSize { get; set; }

        public List<TrackSegment> Segments { get; set; } = new();

        public List<SpeedTraceDto> Drivers { get; set; } = new();
    }

    public sealed class SpeedTraceDto
    {
        public string Id { get; set; }

        public int Lap { get; set; }

        public double? Time { get; set; }

        public List<double?> Speed { get; set; }
    }

    public sealed class TrajectoriesDto
    {
        public TrackSegment Segment { get; set; }

        public List<TrajectoryDto> Drivers { get; set; } = new();
    }

    public sealed class TrajectoryDto
    {
        public string Id { get; set; }

        public int Lap { get; set; }

        public double? Time { get; set; }

        public double SampleHz { get; set; }

        /// <summary>[x, z, vitesse km/h].</summary>
        public List<double[]> Points { get; set; } = new();
    }
}

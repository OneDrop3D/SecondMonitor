namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Découpe le circuit en virages et lignes droites à partir d'un profil de vitesse de référence :
    /// chaque minimum local marqué (≥ <see cref="ProminenceKph"/>) est un virage, chaque maximum la fin d'une ligne droite.
    /// - Vmin d'un virage = vitesse minimale entre les deux pointes de vitesse qui l'encadrent ;
    /// - Vmax d'une ligne droite = vitesse maximale entre les deux virages qui l'encadrent.
    /// Ces fenêtres larges rendent la mesure robuste, même avec des données peu fréquentes.
    /// </summary>
    public static class TrackSegmenter
    {
        public const double ProminenceKph = 12;

        public static List<TrackSegment> Detect(float[] speed, double lapLength)
        {
            int n = speed?.Length ?? 0;
            if (n < 20 || lapLength <= 0)
            {
                return new List<TrackSegment>();
            }

            double[] v = FillGaps(speed);
            if (v == null)
            {
                return new List<TrackSegment>();
            }

            v = Smooth(v, 3);
            double bin = lapLength / n;

            // Extrema alternés (hystérésis), en partant de la vitesse maximale du tour.
            int start = Array.IndexOf(v, v.Max());
            var minima = new List<int>();
            var maxima = new List<int> { start };
            bool lookingForMin = true;
            int candidate = start;
            for (int k = 1; k <= n; k++)
            {
                int i = (start + k) % n;
                if (lookingForMin)
                {
                    if (v[i] < v[candidate])
                    {
                        candidate = i;
                    }
                    else if (v[i] > v[candidate] + ProminenceKph)
                    {
                        minima.Add(candidate);
                        lookingForMin = false;
                        candidate = i;
                    }
                }
                else
                {
                    if (v[i] > v[candidate])
                    {
                        candidate = i;
                    }
                    else if (v[i] < v[candidate] - ProminenceKph)
                    {
                        maxima.Add(candidate);
                        lookingForMin = true;
                        candidate = i;
                    }
                }
            }

            if (lookingForMin && candidate != start && v[maxima[0]] - v[candidate] >= ProminenceKph)
            {
                minima.Add(candidate);
            }

            // On veut exactement : max0, min0, max1, min1, …, maxK, minK (puis retour à max0).
            if (maxima.Count > minima.Count)
            {
                maxima.RemoveRange(minima.Count, maxima.Count - minima.Count);
            }

            if (minima.Count < 2)
            {
                return new List<TrackSegment>();
            }

            int count = minima.Count;
            var corners = new List<TrackSegment>();
            var straights = new List<TrackSegment>();
            for (int c = 0; c < count; c++)
            {
                int apex = minima[c];
                int maxBefore = maxima[c];
                int maxAfter = maxima[(c + 1) % count];
                double vmin = v[apex];
                double edge = vmin + (0.6 * (Math.Min(v[maxBefore], v[maxAfter]) - vmin));
                int from = Expand(v, apex, -1, edge, maxBefore);
                int to = Expand(v, apex, +1, edge, maxAfter);
                corners.Add(new TrackSegment
                {
                    Kind = "corner",
                    ApexBin = apex,
                    FromBin = from,
                    ToBin = to,
                    MeasureFromBin = maxBefore,
                    MeasureToBin = maxAfter,
                    ReferenceSpeed = Math.Round(vmin, 1),
                });
            }

            for (int c = 0; c < count; c++)
            {
                int top = maxima[c];
                int minBefore = minima[(c - 1 + count) % count];
                int minAfter = minima[c];
                double vmax = v[top];
                double edge = vmax - (0.35 * (vmax - Math.Max(v[minBefore], v[minAfter])));
                straights.Add(new TrackSegment
                {
                    Kind = "straight",
                    ApexBin = top,
                    FromBin = Expand(v, top, -1, edge, minBefore, above: true),
                    ToBin = Expand(v, top, +1, edge, minAfter, above: true),
                    MeasureFromBin = minBefore,
                    MeasureToBin = minAfter,
                    ReferenceSpeed = Math.Round(vmax, 1),
                });
            }

            // Numérotation depuis la ligne de départ.
            var orderedCorners = corners.OrderBy(s => s.ApexBin).ToList();
            for (int i = 0; i < orderedCorners.Count; i++)
            {
                orderedCorners[i].Id = $"V{i + 1}";
                orderedCorners[i].Name = $"Virage {i + 1}";
            }

            var orderedStraights = straights.OrderBy(s => s.ApexBin).ToList();
            double best = orderedStraights.Max(s => s.ReferenceSpeed);
            for (int i = 0; i < orderedStraights.Count; i++)
            {
                TrackSegment s = orderedStraights[i];
                s.Id = $"LD{i + 1}";
                TrackSegment next = orderedCorners.FirstOrDefault(c => c.ApexBin > s.ApexBin) ?? orderedCorners[0];
                s.Name = Math.Abs(s.ReferenceSpeed - best) < 0.01 ? $"Ligne droite {i + 1} (principale)" : $"Ligne droite {i + 1}";
                s.Description = $"avant le {next.Name.ToLowerInvariant()}";
                s.IsMain = Math.Abs(s.ReferenceSpeed - best) < 0.01;
            }

            foreach (TrackSegment s in orderedCorners.Concat(orderedStraights))
            {
                s.Start = Math.Round(s.FromBin * bin, 0);
                s.End = Math.Round((s.ToBin + 1) * bin, 0);
                s.Apex = Math.Round((s.ApexBin + 0.5) * bin, 0);
            }

            return orderedCorners.Concat(orderedStraights).OrderBy(s => s.ApexBin).ToList();
        }

        /// <summary>Valeur (Vmin pour un virage, Vmax pour une ligne droite) mesurée sur une trace de tour.</summary>
        public static double? Measure(float[] speed, TrackSegment segment)
        {
            int n = speed.Length;
            double best = segment.Kind == "corner" ? double.MaxValue : double.MinValue;
            bool any = false;
            int i = segment.MeasureFromBin;
            int guard = 0;
            while (guard++ <= n)
            {
                float s = speed[i % n];
                if (!float.IsNaN(s))
                {
                    any = true;
                    best = segment.Kind == "corner" ? Math.Min(best, s) : Math.Max(best, s);
                }

                if (i % n == segment.MeasureToBin % n)
                {
                    break;
                }

                i++;
            }

            return any ? Math.Round(best, 1) : null;
        }

        private static int Expand(double[] v, int from, int dir, double edge, int limit, bool above = false)
        {
            int n = v.Length;
            int i = from;
            for (int k = 0; k < n; k++)
            {
                int next = (i + dir + n) % n;
                if (i == limit || (above ? v[next] < edge : v[next] > edge))
                {
                    break;
                }

                i = next;
            }

            return i;
        }

        private static double[] FillGaps(float[] speed)
        {
            int n = speed.Length;
            int valid = speed.Count(s => !float.IsNaN(s));
            if (valid < n * 0.8)
            {
                return null;
            }

            var v = new double[n];
            for (int i = 0; i < n; i++)
            {
                if (!float.IsNaN(speed[i]))
                {
                    v[i] = speed[i];
                    continue;
                }

                int a = i;
                int b = i;
                int k = 0;
                while (float.IsNaN(speed[a]) && k++ < n)
                {
                    a = (a - 1 + n) % n;
                }

                k = 0;
                while (float.IsNaN(speed[b]) && k++ < n)
                {
                    b = (b + 1) % n;
                }

                int da = (i - a + n) % n;
                int db = (b - i + n) % n;
                v[i] = da + db == 0 ? speed[a] : speed[a] + ((speed[b] - speed[a]) * da / (double)(da + db));
            }

            return v;
        }

        private static double[] Smooth(double[] v, int half)
        {
            int n = v.Length;
            var r = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int k = -half; k <= half; k++)
                {
                    sum += v[(i + k + n) % n];
                }

                r[i] = sum / ((2 * half) + 1);
            }

            return r;
        }
    }

    public sealed class TrackSegment
    {
        public string Id { get; set; }

        /// <summary>« corner » (virage) ou « straight » (ligne droite).</summary>
        public string Kind { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>Nom du lieu sur un circuit connu (Hunaudières, Mulsanne…, voir TrackPlaces), sinon null.</summary>
        public string Place { get; set; }

        public bool IsMain { get; set; }

        /// <summary>Début et fin de la zone (m depuis la ligne ; Start &gt; End si la zone chevauche la ligne).</summary>
        public double Start { get; set; }

        public double End { get; set; }

        /// <summary>Point de corde (virage) ou de vitesse maximale (ligne droite), en m.</summary>
        public double Apex { get; set; }

        /// <summary>Vitesse du tour de référence (km/h).</summary>
        public double ReferenceSpeed { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int ApexBin { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int FromBin { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int ToBin { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int MeasureFromBin { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int MeasureToBin { get; set; }
    }
}

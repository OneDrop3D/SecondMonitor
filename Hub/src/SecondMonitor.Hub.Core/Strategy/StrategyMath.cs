namespace SecondMonitor.Hub.Core.Strategy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class StrategyMath
    {
        /// <summary>Moyenne robuste : écarte les valeurs aberrantes (trafic, drapeau jaune, erreur) au-delà de ±tolérance autour de la médiane.</summary>
        public static double? RobustAverage(IEnumerable<double> values, double tolerance = 0.07)
        {
            var list = values.Where(v => v > 0 && !double.IsNaN(v)).ToList();
            if (list.Count == 0)
            {
                return null;
            }

            double median = Median(list);
            var kept = list.Where(v => Math.Abs(v - median) <= median * tolerance).ToList();
            return kept.Count == 0 ? median : kept.Average();
        }

        public static double Median(IReadOnlyCollection<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int n = sorted.Count;
            return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2.0;
        }

        /// <summary>Pente de régression linéaire des temps au tour (s/tour). Nécessite au moins 4 tours propres.</summary>
        public static double? DegradationSlope(IReadOnlyList<(int Lap, double Time)> laps) => LinearFit(laps)?.Slope;

        /// <summary>Régression linéaire temps = pente × tour + origine, tours aberrants (±3 % de la médiane) écartés. Au moins 4 tours.</summary>
        public static (double Slope, double Intercept, int Count)? LinearFit(IReadOnlyList<(int Lap, double Time)> laps)
        {
            if (laps.Count < 4)
            {
                return null;
            }

            double median = Median(laps.Select(l => l.Time).ToList());
            var kept = laps.Where(l => l.Time <= median * 1.03 && l.Time >= median * 0.97).ToList();
            if (kept.Count < 4)
            {
                return null;
            }

            double meanX = kept.Average(l => (double)l.Lap);
            double meanY = kept.Average(l => l.Time);
            double num = kept.Sum(l => (l.Lap - meanX) * (l.Time - meanY));
            double den = kept.Sum(l => (l.Lap - meanX) * (l.Lap - meanX));
            if (den <= 1e-9)
            {
                return null;
            }

            double slope = num / den;
            return (slope, meanY - (slope * meanX), kept.Count);
        }
    }
}

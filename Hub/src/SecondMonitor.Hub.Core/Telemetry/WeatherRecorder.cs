namespace SecondMonitor.Hub.Core.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;

    /// <summary>
    /// Historique météo de la session (toutes les 10 s de jeu) et tendance pour les 30 prochaines minutes.
    /// Le protocole SecondMonitor ne transmet pas les prévisions officielles des jeux : la « prévision » est une
    /// extrapolation prudente de l'évolution récente (régression linéaire sur 10 min), signalée comme telle.
    /// </summary>
    public sealed class WeatherRecorder
    {
        public const double Interval = 10;
        private const double TrendWindow = 600;
        private readonly List<WeatherSample> _samples = new();
        private double _last = double.NegativeInfinity;

        /// <summary>Phrase de tendance, recalculée à chaque mesure (pour l'IA et le flux temps réel).</summary>
        public string TrendSummary { get; private set; }

        public void Reset()
        {
            TrendSummary = null;
            _samples.Clear();
            _last = double.NegativeInfinity;
        }

        public void Add(double now, WeatherInfo w)
        {
            if (w == null || now < _last + Interval)
            {
                return;
            }

            _last = now;
            _samples.Add(new WeatherSample
            {
                T = Math.Round(now, 0),
                Air = Temp(w.AirTemperature),
                Track = Temp(w.TrackTemperature),
                Rain = w.RainIntensity,
                Wetness = w.TrackWetness,
                Wind = w.HasWindInformation ? Math.Round(w.WindSpeed?.InKph ?? 0, 1) : null,
                WindFrom = w.HasWindInformation ? Math.Round(w.WindDirectionFrom, 0) : null,
            });

            if (_samples.Count > 20000)
            {
                _samples.RemoveRange(0, 5000);
            }

            WeatherReportDto report = Build(now);
            TrendSummary = report.Forecast.Count > 0 ? report.Summary : null;
            if (TrendSummary != null && report.Forecast.Count == 3)
            {
                WeatherSample in30 = report.Forecast[2];
                TrendSummary += $" Dans 30 minutes (extrapolation) : piste vers {in30.Track:0} degrés, pluie {in30.Rain} %.";
            }
        }

        public WeatherReportDto Build(double now)
        {
            var report = new WeatherReportDto { History = _samples.ToList() };
            WeatherSample last = _samples.LastOrDefault();
            if (last == null)
            {
                return report;
            }

            var recent = _samples.Where(s => s.T >= last.T - TrendWindow).ToList();
            double span = recent.Count >= 2 ? recent[^1].T - recent[0].T : 0;
            report.TrendMinutes = Math.Round(span / 60, 1);
            if (span < 120)
            {
                report.Summary = "Pas encore assez d'historique pour dégager une tendance (2 minutes minimum).";
                return report;
            }

            double? airSlope = Slope(recent, s => s.Air);
            double? trackSlope = Slope(recent, s => s.Track);
            double? rainSlope = Slope(recent, s => s.Rain);
            double? wetSlope = Slope(recent, s => s.Wetness);
            report.TrackPerHour = trackSlope.HasValue ? Math.Round(trackSlope.Value * 3600, 1) : null;
            report.AirPerHour = airSlope.HasValue ? Math.Round(airSlope.Value * 3600, 1) : null;
            report.RainPerHour = rainSlope.HasValue ? Math.Round(rainSlope.Value * 3600, 0) : null;
            report.WetnessPerHour = wetSlope.HasValue ? Math.Round(wetSlope.Value * 3600, 1) : null;

            foreach (int minutes in new[] { 10, 20, 30 })
            {
                double dt = minutes * 60;
                // Amortissement : on ne prolonge pas une pente indéfiniment (pas plus de ±8 °C, pluie bornée 0–100 %).
                report.Forecast.Add(new WeatherSample
                {
                    T = Math.Round(last.T + dt, 0),
                    Air = Project(last.Air, airSlope, dt, 8, -30, 60),
                    Track = Project(last.Track, trackSlope, dt, 8, -30, 80),
                    Rain = (int)Math.Round(Project(last.Rain, rainSlope, dt, 25, 0, 100) ?? last.Rain),
                    Wetness = (int)Math.Round(Project(last.Wetness, wetSlope, dt, 30, 0, 100) ?? last.Wetness),
                    Wind = last.Wind,
                    WindFrom = last.WindFrom,
                });
            }

            var words = new List<string>();
            if (trackSlope is double ts)
            {
                double perTen = ts * 600;
                words.Add(Math.Abs(perTen) < 0.3 ? "piste stable" : perTen > 0 ? $"piste qui chauffe (+{perTen:0.0} °C / 10 min)" : $"piste qui refroidit ({perTen:0.0} °C / 10 min)");
            }

            if (rainSlope is double rs && (last.Rain > 0 || rs > 0))
            {
                double perTen = rs * 600;
                words.Add(perTen > 2 ? "pluie qui s'intensifie" : perTen < -2 ? "pluie qui faiblit" : last.Rain > 0 ? "pluie stable" : "temps sec");
            }
            else
            {
                words.Add("temps sec");
            }

            if (wetSlope is double ws && last.Wetness > 0)
            {
                words.Add(ws * 600 > 2 ? "piste de plus en plus mouillée" : ws * 600 < -2 ? "piste qui sèche" : "humidité stable");
            }

            report.Summary = "Tendance : " + string.Join(", ", words) + ".";
            return report;
        }

        private static double? Project(double? value, double? slope, double dt, double maxChange, double min, double max)
        {
            if (!value.HasValue)
            {
                return null;
            }

            double change = Math.Clamp((slope ?? 0) * dt, -maxChange, maxChange);
            return Math.Round(Math.Clamp(value.Value + change, min, max), 1);
        }

        private static double? Slope(List<WeatherSample> samples, Func<WeatherSample, double?> pick)
        {
            var pts = samples.Where(s => pick(s).HasValue).Select(s => (X: s.T, Y: pick(s).Value)).ToList();
            if (pts.Count < 4)
            {
                return null;
            }

            double mx = pts.Average(p => p.X);
            double my = pts.Average(p => p.Y);
            double den = pts.Sum(p => (p.X - mx) * (p.X - mx));
            return den <= 1e-9 ? null : pts.Sum(p => (p.X - mx) * (p.Y - my)) / den;
        }

        private static double? Temp(Temperature t)
        {
            if (t == null || t.IsZero)
            {
                return null;
            }

            double c = t.InCelsius;
            return Math.Abs(c + 1) < 0.001 || c < -40 || c > 90 ? null : Math.Round(c, 1);
        }
    }

    public sealed class WeatherSample
    {
        public double T { get; set; }

        public double? Air { get; set; }

        public double? Track { get; set; }

        public int Rain { get; set; }

        public int Wetness { get; set; }

        public double? Wind { get; set; }

        public double? WindFrom { get; set; }
    }

    public sealed class WeatherReportDto
    {
        public List<WeatherSample> History { get; set; } = new();

        public List<WeatherSample> Forecast { get; set; } = new();

        public double? TrackPerHour { get; set; }

        public double? AirPerHour { get; set; }

        public double? RainPerHour { get; set; }

        public double? WetnessPerHour { get; set; }

        public double TrendMinutes { get; set; }

        public string Summary { get; set; }
    }
}

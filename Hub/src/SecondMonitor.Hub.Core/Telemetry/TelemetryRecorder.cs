namespace SecondMonitor.Hub.Core.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    /// <summary>
    /// Historique (une mesure par seconde de jeu) de la voiture du joueur et de la piste, pour les graphiques :
    /// températures des pneus (surface et carcasse), des freins, du moteur (eau, huile), pressions, piste et air.
    /// Stockage en colonnes pour un JSON compact ; on ne renvoie que les nouvelles mesures (paramètre since).
    /// </summary>
    public sealed class TelemetryRecorder
    {
        public const double Interval = 1.0;
        public const int MaxSamples = 3 * 3600;

        public static readonly string[] Keys =
        {
            "lap",
            "speed",
            "tyreFL", "tyreFR", "tyreRL", "tyreRR",
            "coreFL", "coreFR", "coreRL", "coreRR",
            "brakeFL", "brakeFR", "brakeRL", "brakeRR",
            "pressFL", "pressFR", "pressRL", "pressRR",
            "wearFL", "wearFR", "wearRL", "wearRR",
            "water", "oil", "oilPressure",
            "track", "air",
            "fuel", "energy",
        };

        private readonly List<double> _time = new();
        private readonly Dictionary<string, List<double?>> _series = Keys.ToDictionary(k => k, _ => new List<double?>());
        private double _lastSample = double.NegativeInfinity;

        public int SessionId { get; private set; } = 1;

        public void Reset()
        {
            _time.Clear();
            foreach (var list in _series.Values)
            {
                list.Clear();
            }

            _lastSample = double.NegativeInfinity;
            SessionId++;
        }

        public void Add(double now, DriverInfo player, SessionInfo session)
        {
            if (player?.CarInfo == null || now < _lastSample + Interval)
            {
                return;
            }

            _lastSample = now;
            CarInfo car = player.CarInfo;
            Wheels w = car.WheelsInfo;
            WheelInfo[] wheels = { w?.FrontLeft, w?.FrontRight, w?.RearLeft, w?.RearRight };
            string[] names = { "FL", "FR", "RL", "RR" };

            _time.Add(Math.Round(now, 1));
            Put("lap", player.CompletedLaps + (session?.TrackInfo?.LayoutLength?.InMeters > 0 ? Math.Clamp(player.LapDistance / session.TrackInfo.LayoutLength.InMeters, 0, 1) : 0));
            Put("speed", player.Speed?.InKph);
            for (int i = 0; i < 4; i++)
            {
                WheelInfo wi = wheels[i];
                double?[] surface = { Temp(wi?.LeftTyreTemp), Temp(wi?.CenterTyreTemp), Temp(wi?.RightTyreTemp) };
                var valid = surface.Where(s => s.HasValue).Select(s => s.Value).ToList();
                Put("tyre" + names[i], valid.Count == 0 ? null : valid.Average());
                Put("core" + names[i], Temp(wi?.TyreCoreTemperature));
                Put("brake" + names[i], Temp(wi?.BrakeTemperature));
                double? p = wi?.TyrePressure?.ActualQuantity?.InKpa;
                Put("press" + names[i], p > 0 ? p : null);
                Put("wear" + names[i], wi?.TyreWear != null ? wi.TyreWear.ActualWear * 100 : null);
            }

            Put("water", Temp(car.WaterSystemInfo?.OptimalWaterTemperature));
            Put("oil", Temp(car.OilSystemInfo?.OptimalOilTemperature));
            double? oilP = car.OilSystemInfo?.OilPressure?.InKpa;
            Put("oilPressure", oilP > 0 ? oilP : null);
            Put("track", TempOf(session?.WeatherInfo?.TrackTemperature));
            Put("air", TempOf(session?.WeatherInfo?.AirTemperature));
            double? fuel = car.FuelSystemInfo?.FuelRemaining?.InLiters;
            Put("fuel", fuel > 0 ? fuel : null);
            Put("energy", car.FuelSystemInfo?.HasVirtualTank == true ? car.FuelSystemInfo.VirtualTank.InRatio * 100 : null);

            if (_time.Count > MaxSamples)
            {
                int drop = _time.Count - MaxSamples;
                _time.RemoveRange(0, drop);
                foreach (var list in _series.Values)
                {
                    list.RemoveRange(0, drop);
                }
            }
        }

        /// <summary>Mesures postérieures à <paramref name="since"/> ; les séries entièrement vides sont omises.</summary>
        public TelemetryHistoryDto Read(double since, int maxPoints = 4000)
        {
            int start = _time.FindIndex(t => t > since);
            var dto = new TelemetryHistoryDto { SessionId = SessionId };
            if (start < 0)
            {
                return dto;
            }

            int count = _time.Count - start;
            int step = Math.Max(1, (int)Math.Ceiling(count / (double)maxPoints));
            var idx = Enumerable.Range(0, count).Where(i => i % step == 0 || i == count - 1).Select(i => start + i).ToList();
            dto.T = idx.Select(i => _time[i]).ToList();
            foreach (var (key, list) in _series)
            {
                if (idx.Any(i => list[i].HasValue))
                {
                    dto.Series[key] = idx.Select(i => list[i].HasValue ? Math.Round(list[i].Value, 2) : (double?)null).ToList();
                }
            }

            return dto;
        }

        private void Put(string key, double? value) => _series[key].Add(value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value) ? value : null);

        private static double? Temp(OptimalQuantity<Temperature> q) => TempOf(q?.ActualQuantity);

        private static double? TempOf(Temperature t)
        {
            if (t == null || t.IsZero)
            {
                return null;
            }

            double c = t.InCelsius;
            return Math.Abs(c + 1) < 0.001 ? null : c is > -40 and < 1500 ? c : null; // -1 °C = « non fourni » dans SecondMonitor
        }
    }

    public sealed class TelemetryHistoryDto
    {
        public int SessionId { get; set; }

        public List<double> T { get; set; } = new();

        public Dictionary<string, List<double?>> Series { get; set; } = new();
    }
}

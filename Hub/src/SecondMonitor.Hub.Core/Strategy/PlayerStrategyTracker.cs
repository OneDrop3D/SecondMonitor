namespace SecondMonitor.Hub.Core.Strategy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Consommation (carburant, énergie virtuelle), usure des pneus par tour et projection jusqu'à l'arrivée pour la voiture du joueur.
    /// </summary>
    public sealed class PlayerStrategyTracker
    {
        public const double FuelMarginLaps = 1.0;

        private int _lastLap = -1;
        private double? _fuelAtLapStart;
        private double? _energyAtLapStart;
        private double[] _wearAtLapStart;
        private readonly List<LapConsumption> _history = new();
        private bool _partialLap;
        private readonly HybridTracker _hybrid = new();
        private readonly EnergyLapTrace _trace = new();

        /// <summary>Virages et lignes droites nommés du circuit (pour dire où la batterie se vide et se recharge).</summary>
        public IReadOnlyList<TrackSegment> Segments { get; set; }

        public IReadOnlyList<LapConsumption> History => _history;

        public void Reset()
        {
            _lastLap = -1;
            _partialLap = false;
            _fuelAtLapStart = null;
            _energyAtLapStart = null;
            _wearAtLapStart = null;
            _history.Clear();
            _hybrid.Reset();
            _trace.Reset();
        }

        /// <summary>
        /// Consommation tour par tour et repères du tour en cours, pour reprendre la course après un redémarrage.
        /// Les repères (essence, énergie, usure au début du tour) restent justes : le jeu, lui, ne s'est pas arrêté.
        /// La batterie hybride et la trace du tour en cours ne sont pas gardées (elles se refont en un tour).
        /// </summary>
        public PlayerStrategyState Export() => new()
        {
            LastLap = _lastLap,
            FuelAtLapStart = _fuelAtLapStart,
            EnergyAtLapStart = _energyAtLapStart,
            WearAtLapStart = (double[])_wearAtLapStart?.Clone(),
            History = _history.Select(h => h.Copy()).ToList(),
        };

        public void Import(PlayerStrategyState state)
        {
            Reset();
            if (state == null)
            {
                return;
            }

            _lastLap = state.LastLap;
            _fuelAtLapStart = state.FuelAtLapStart;
            _energyAtLapStart = state.EnergyAtLapStart;
            _wearAtLapStart = state.WearAtLapStart?.Length == 4 ? state.WearAtLapStart : null;
            _history.AddRange((state.History ?? new List<LapConsumption>()).Where(h => h != null));
        }

        public void Update(DriverInfo player, DriverTracker tracker)
        {
            if (player?.CarInfo == null)
            {
                return;
            }

            _hybrid.Update(player, Environment.TickCount64 / 1000.0);
            FuelInfo fuel = player.CarInfo.FuelSystemInfo;
            double fuelNow = fuel?.FuelRemaining?.InLiters ?? 0;
            double? energyNow = fuel?.HasVirtualTank == true ? fuel.VirtualTank.InRatio * 100 : null;
            double[] wearNow = ReadWear(player.CarInfo.WheelsInfo);
            HybridSystem hs = player.CarInfo.HybridSystem;
            double? batteryNow = hs != null && HybridTracker.HasHybrid(hs) ? Math.Clamp(hs.RemainingChargePercentage, 0, 1) * 100 : null;
            _trace.Update(player.CompletedLaps, player.LapDistance, batteryNow, energyNow, fuel?.FuelCapacity?.InLiters > 0 ? fuelNow : null, player.InPits);

            if (_lastLap < 0)
            {
                _lastLap = player.CompletedLaps;
                // Démarré en plein tour (application lancée ou relancée en course) : ce premier tour n'est mesuré qu'en partie,
                // il ne compte pas dans la consommation au tour (on lisait 1,5 L au lieu de 7,3 au Mans).
                _partialLap = player.LapDistance > 150;
                _fuelAtLapStart = fuelNow;
                _energyAtLapStart = energyNow;
                _wearAtLapStart = wearNow;
                return;
            }

            if (player.CompletedLaps <= _lastLap)
            {
                if (player.CompletedLaps < _lastLap)
                {
                    Reset();
                }

                return;
            }

            LapRecord lap = tracker?.Laps.LastOrDefault();
            bool pitLap = lap?.IsPitLap == true || player.InPits;
            var consumption = new LapConsumption { Lap = player.CompletedLaps, PitLap = pitLap || player.CompletedLaps != _lastLap + 1 || _partialLap };
            _partialLap = false;

            if (_fuelAtLapStart.HasValue)
            {
                double used = _fuelAtLapStart.Value - fuelNow;
                consumption.Fuel = used > 0 ? used : null; // négatif = ravitaillement
            }

            if (_energyAtLapStart.HasValue && energyNow.HasValue)
            {
                double used = _energyAtLapStart.Value - energyNow.Value;
                consumption.Energy = used > 0 ? used : null;
            }

            if (_wearAtLapStart != null && wearNow != null)
            {
                var delta = new double[4];
                bool tyreChange = false;
                for (int i = 0; i < 4; i++)
                {
                    delta[i] = wearNow[i] - _wearAtLapStart[i];
                    tyreChange |= delta[i] < -0.5;
                }

                consumption.WearDelta = tyreChange ? null : delta;
            }

            if (lap != null)
            {
                lap.FuelUsed = consumption.Fuel;
                lap.EnergyUsedPct = consumption.Energy;
                lap.WearDeltaPct = consumption.WearDelta;
                consumption.LapTime = lap.Time;
                consumption.Valid = lap.Valid;
                consumption.Compound = lap.Compound;
                consumption.Stint = lap.Stint;
            }

            _history.Add(consumption);
            _lastLap = player.CompletedLaps;
            _fuelAtLapStart = fuelNow;
            _energyAtLapStart = energyNow;
            _wearAtLapStart = wearNow;
        }

        public PlayerStrategy Build(SimulatorDataSet data, DriverInfo player, DriverTracker tracker, double lapLength)
        {
            if (player?.CarInfo == null)
            {
                return null;
            }

            var result = new PlayerStrategy();
            var clean = _history.Where(h => !h.PitLap).TakeLast(8).ToList();
            double? fuelPerLap = StrategyMath.RobustAverage(clean.Where(h => h.Fuel.HasValue).Select(h => h.Fuel.Value), 0.15);
            double? energyPerLap = StrategyMath.RobustAverage(clean.Where(h => h.Energy.HasValue).Select(h => h.Energy.Value), 0.15);
            double? avgLap = tracker?.AverageOfLastCleanLaps(5);
            result.AverageLap = avgLap;
            result.Degradation = tracker?.CurrentStintDegradation();
            result.StintLaps = tracker?.CurrentStint?.Laps ?? 0;

            double? lapsRemaining = EstimateLapsRemaining(data.SessionInfo, player, avgLap, lapLength);
            result.LapsRemaining = lapsRemaining;

            // En direct : ce tour comparé au précédent au même endroit, et une estimation du tour entier.
            result.Live = _trace.Build(lapLength, Segments);
            bool fuelEstimated = false;
            bool energyEstimated = false;
            if (fuelPerLap == null && result.Live?.Fuel?.ProjectedLapChange is double pf && pf < -0.05)
            {
                fuelPerLap = -pf; // pas encore de tour bouclé : estimation sur le tour en cours
                fuelEstimated = true;
            }

            if (energyPerLap == null && result.Live?.VirtualEnergy?.ProjectedLapChange is double pe && pe < -0.05)
            {
                energyPerLap = -pe;
                energyEstimated = true;
            }

            FuelInfo fuel = player.CarInfo.FuelSystemInfo;
            if (fuel != null && fuel.FuelCapacity.InLiters > 0)
            {
                double remaining = fuel.FuelRemaining.InLiters;
                var fs = new FuelStrategy
                {
                    Remaining = remaining,
                    Capacity = fuel.FuelCapacity.InLiters,
                    PerLap = fuelPerLap,
                    PerLapEstimated = fuelEstimated,
                    LapsLeft = fuelPerLap > 0 ? remaining / fuelPerLap : null,
                };

                if (fuelPerLap > 0 && lapsRemaining.HasValue)
                {
                    double needed = (lapsRemaining.Value + FuelMarginLaps) * fuelPerLap.Value;
                    fs.ToAdd = needed - remaining;
                    fs.StopsNeeded = fs.ToAdd <= 0 ? 0 : (int)Math.Ceiling(fs.ToAdd.Value / fs.Capacity);
                    fs.Status = fs.ToAdd <= 0 ? "ok" : (fs.LapsLeft < 2 ? "critical" : "short");
                }
                else
                {
                    fs.Status = "learning";
                }

                result.Fuel = fs;
            }

            if (fuel?.HasVirtualTank == true)
            {
                double remainingPct = fuel.VirtualTank.InRatio * 100;
                var es = new EnergyStrategy
                {
                    RemainingPct = remainingPct,
                    PerLapPct = energyPerLap,
                    PerLapEstimated = energyEstimated,
                    LapsLeft = energyPerLap > 0 ? remainingPct / energyPerLap : null,
                };

                if (energyPerLap > 0 && lapsRemaining.HasValue)
                {
                    es.ToAddPct = ((lapsRemaining.Value + FuelMarginLaps) * energyPerLap.Value) - remainingPct;
                    es.Status = es.ToAddPct <= 0 ? "ok" : (es.LapsLeft < 2 ? "critical" : "short");
                }
                else
                {
                    es.Status = "learning";
                }

                result.Energy = es;
            }

            BuildWheels(result, player.CarInfo.WheelsInfo, clean);

            result.Engine = BuildEngine(player.CarInfo);

            HybridSystem hybrid = player.CarInfo.HybridSystem;
            if (hybrid != null && hybrid.RemainingChargePercentage > 0)
            {
                result.Hybrid = new HybridDto { ChargePct = hybrid.RemainingChargePercentage * 100, Mode = hybrid.HybridSystemMode.ToString() };
            }

            // Batterie suivie tour par tour (même vide : une batterie à 0 % reste une batterie).
            result.Hybrid = _hybrid.Fill(result.Hybrid, hybrid, data?.Source);
            if (result.Hybrid != null)
            {
                result.Hybrid.ChargePct = Math.Clamp(hybrid.RemainingChargePercentage, 0, 1) * 100;
            }

            // Arrêt recommandé = première limite atteinte (carburant, énergie, pneus usés).
            var limits = new List<(double Laps, string Reason)>();
            if (result.Fuel?.LapsLeft is double fl && (result.Fuel.ToAdd ?? 0) > 0)
            {
                limits.Add((fl - FuelMarginLaps / 2, "carburant"));
            }

            if (result.Energy?.LapsLeft is double el && (result.Energy.ToAddPct ?? 0) > 0)
            {
                limits.Add((el - FuelMarginLaps / 2, "énergie virtuelle"));
            }

            double? tyreLaps = result.Wheels.Where(w => w.LapsToHeavyWear.HasValue).Select(w => w.LapsToHeavyWear).Min();
            if (tyreLaps.HasValue && (!lapsRemaining.HasValue || tyreLaps < lapsRemaining))
            {
                limits.Add((tyreLaps.Value, "usure des pneus"));
            }

            if (limits.Count > 0)
            {
                var first = limits.OrderBy(l => l.Laps).First();
                result.RecommendedStopLap = player.CompletedLaps + Math.Max(0, (int)Math.Floor(first.Laps));
                result.RecommendedStopReason = first.Reason;
            }

            // Quantités à mettre au prochain arrêt : de quoi finir, sans dépasser la capacité.
            int lapsUntilStop = result.RecommendedStopLap.HasValue ? Math.Max(0, result.RecommendedStopLap.Value - player.CompletedLaps) : 0;
            if (result.Fuel is { PerLap: > 0 } f && lapsRemaining.HasValue && (f.ToAdd ?? 0) > 0)
            {
                double arrival = Math.Max(0, f.Remaining - (f.PerLap.Value * lapsUntilStop));
                double needAfter = (Math.Max(0, lapsRemaining.Value - lapsUntilStop) + FuelMarginLaps) * f.PerLap.Value;
                f.NextStopAdd = Math.Max(0, Math.Min(f.Capacity - arrival, needAfter - arrival));
            }

            if (result.Energy is { PerLapPct: > 0 } e && lapsRemaining.HasValue && (e.ToAddPct ?? 0) > 0)
            {
                double arrival = Math.Max(0, e.RemainingPct - (e.PerLapPct.Value * lapsUntilStop));
                double needAfter = (Math.Max(0, lapsRemaining.Value - lapsUntilStop) + FuelMarginLaps) * e.PerLapPct.Value;
                e.NextStopAddPct = Math.Max(0, Math.Min(100 - arrival, needAfter - arrival));
            }

            result.RecentLaps = Laps(12);
            return result;
        }

        /// <summary>Tours du joueur avec consommation et usure par tour (les <paramref name="count"/> derniers).</summary>
        public List<LapDto> Laps(int count = int.MaxValue) => _history.TakeLast(count).Select(h => new LapDto
        {
            Lap = h.Lap,
            Time = h.LapTime,
            Fuel = h.Fuel,
            Energy = h.Energy,
            Pit = h.PitLap,
            Valid = h.Valid,
            WearDelta = h.WearDelta?.Select(v => Math.Round(v, 2)).ToArray(),
            Compound = h.Compound,
            Stint = h.Stint,
        }).ToList();

        private static EngineDto BuildEngine(CarInfo car)
        {
            var e = new EngineDto
            {
                WaterTemp = Temp(car.WaterSystemInfo?.OptimalWaterTemperature),
                OilTemp = Temp(car.OilSystemInfo?.OptimalOilTemperature),
                OilPressure = NonZero(car.OilSystemInfo?.OilPressure?.InKpa),
                Rpm = car.EngineRpm,
                Gear = car.CurrentGear,
            };
            if (e.WaterTemp.HasValue)
            {
                e.WaterIdeal = Ideal(car.WaterSystemInfo.OptimalWaterTemperature);
                e.WaterWindow = Window(car.WaterSystemInfo.OptimalWaterTemperature);
            }

            if (e.OilTemp.HasValue)
            {
                e.OilIdeal = Ideal(car.OilSystemInfo.OptimalOilTemperature);
                e.OilWindow = Window(car.OilSystemInfo.OptimalOilTemperature);
            }

            CarDamageInformation dmg = car.CarDamageInformation;
            if (dmg != null)
            {
                e.DamageEngine = Damage(dmg.Engine);
                e.DamageTransmission = Damage(dmg.Transmission);
                e.DamageSuspension = Damage(dmg.Suspension);
                e.DamageBodywork = Damage(dmg.Bodywork);
            }

            return e.WaterTemp == null && e.OilTemp == null && e.Rpm == 0 && e.DamageEngine == null ? null : e;
        }

        private static double? Damage(DamageInformation d) => d == null ? null : Math.Round(Math.Clamp(d.Damage, 0, 1) * 100, 1);

        private static double? Ideal(OptimalQuantity<Temperature> q) => q?.IdealQuantity == null || q.IdealQuantity.IsZero ? null : q.IdealQuantity.InCelsius;

        private static double? Window(OptimalQuantity<Temperature> q) => q?.IdealQuantityWindow == null || q.IdealQuantityWindow.IsZero ? null : q.IdealQuantityWindow.InCelsius;

        public static double? EstimateLapsRemaining(SessionInfo session, DriverInfo player, double? avgLap, double lapLength)
        {
            if (session == null || session.SessionType != SessionType.Race)
            {
                return null;
            }

            double fraction = lapLength > 0 ? Math.Clamp(player.LapDistance / lapLength, 0, 1) : 0;
            switch (session.SessionLengthType)
            {
                case SessionLengthType.Laps when session.TotalNumberOfLaps > 0:
                    return Math.Max(0, session.TotalNumberOfLaps - player.CompletedLaps - fraction);
                case SessionLengthType.Time:
                case SessionLengthType.TimeWithExtraLap:
                    if (avgLap is not > 0 || session.SessionTimeRemaining <= 0)
                    {
                        return null;
                    }

                    // On termine le tour en cours quand le temps est écoulé (+1 si tour supplémentaire).
                    double laps = Math.Ceiling((session.SessionTimeRemaining / avgLap.Value) + fraction) - fraction;
                    if (session.SessionLengthType == SessionLengthType.TimeWithExtraLap)
                    {
                        laps += 1;
                    }

                    return Math.Max(0, laps);
                default:
                    return null;
            }
        }

        private static double[] ReadWear(Wheels wheels)
        {
            if (wheels == null)
            {
                return null;
            }

            var w = new[] { wheels.FrontLeft, wheels.FrontRight, wheels.RearLeft, wheels.RearRight };
            if (w.Any(x => x?.TyreWear == null))
            {
                return null;
            }

            return w.Select(x => x.TyreWear.ActualWear * 100).ToArray();
        }

        private static void BuildWheels(PlayerStrategy result, Wheels wheels, List<LapConsumption> clean)
        {
            if (wheels == null)
            {
                return;
            }

            string[] names = { "FL", "FR", "RL", "RR" };
            WheelInfo[] list = { wheels.FrontLeft, wheels.FrontRight, wheels.RearLeft, wheels.RearRight };
            var wearHistory = clean.Where(c => c.WearDelta != null).TakeLast(5).ToList();
            for (int i = 0; i < 4; i++)
            {
                WheelInfo w = list[i];
                if (w == null)
                {
                    continue;
                }

                double? perLap = wearHistory.Count == 0 ? null : StrategyMath.RobustAverage(wearHistory.Select(h => h.WearDelta[i]), 0.5);
                double wearPct = w.TyreWear.ActualWear * 100;
                double heavy = (w.TyreWear.HeavyWearLimit > 0 ? w.TyreWear.HeavyWearLimit : 0.7) * 100;
                result.Wheels.Add(new WheelStatus
                {
                    Position = names[i],
                    Compound = w.TyreType,
                    WearPct = wearPct,
                    WearPerLapPct = perLap,
                    LapsToHeavyWear = perLap > 0.01 ? Math.Max(0, (heavy - wearPct) / perLap.Value) : null,
                    HeavyWearLimitPct = heavy,
                    Pressure = NonZero(w.TyrePressure?.ActualQuantity?.InKpa),
                    IdealPressure = NonZero(w.TyrePressure?.IdealQuantity?.InKpa),
                    TempInner = Temp(w.LeftTyreTemp),
                    TempMiddle = Temp(w.CenterTyreTemp),
                    TempOuter = Temp(w.RightTyreTemp),
                    Core = Temp(w.TyreCoreTemperature),
                    IdealTemp = w.CenterTyreTemp?.IdealQuantity?.IsZero == false ? w.CenterTyreTemp.IdealQuantity.InCelsius : null,
                    TempWindow = Window(w.CenterTyreTemp),
                    Brake = Temp(w.BrakeTemperature),
                    BrakeIdeal = Ideal(w.BrakeTemperature),
                    BrakeWindow = Window(w.BrakeTemperature),
                });
            }
        }

        private static double? Temp(OptimalQuantity<Temperature> q)
        {
            if (q?.ActualQuantity == null || q.ActualQuantity.IsZero)
            {
                return null;
            }

            double c = q.ActualQuantity.InCelsius;
            return c is > -50 and < 1500 ? c : null;
        }

        private static double? NonZero(double? v) => v is > 0 ? v : null;
    }

    public sealed class LapConsumption
    {
        public int Lap { get; set; }

        public bool PitLap { get; set; }

        public bool Valid { get; set; } = true;

        public double? LapTime { get; set; }

        public double? Fuel { get; set; }

        public double? Energy { get; set; }

        public double[] WearDelta { get; set; }

        /// <summary>Gomme et relais du tour : le carnet en tire les moyennes par train de pneus après la course.</summary>
        public string Compound { get; set; }

        public int? Stint { get; set; }

        public LapConsumption Copy()
        {
            var copy = (LapConsumption)MemberwiseClone();
            copy.WearDelta = (double[])WearDelta?.Clone();
            return copy;
        }
    }

    /// <summary>État sauvegardé de <see cref="PlayerStrategyTracker"/> (reprise de course).</summary>
    public sealed class PlayerStrategyState
    {
        public int LastLap { get; set; } = -1;

        public double? FuelAtLapStart { get; set; }

        public double? EnergyAtLapStart { get; set; }

        public double[] WearAtLapStart { get; set; }

        public List<LapConsumption> History { get; set; } = new();
    }
}

namespace SecondMonitor.Hub.Core.Demo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    /// <summary>
    /// Course fictive (multi-catégories, arrêts, usure, carburant, énergie virtuelle) pour développer
    /// et tester le hub sans jeu ni PC Windows. Produit exactement les mêmes objets que les connecteurs.
    /// </summary>
    public sealed class DemoRaceSimulator
    {
        private const double TrackLength = 5200;
        private const double PitLaneTime = 32;
        private const int ProfilePoints = 1040;
        private static readonly DemoTrackProfile Profile = DemoTrackProfile.Build(TrackLength, ProfilePoints);
        private static readonly string[] HyperCars = { "Porsche 963", "Ferrari 499P", "Toyota GR010 Hybrid", "Peugeot 9X8", "Cadillac V-Series.R", "BMW M Hybrid V8", "Alpine A424", "Lamborghini SC63" };
        private static readonly string[] Gt3Cars = { "Porsche 911 GT3 R", "Ferrari 296 GT3", "BMW M4 GT3", "Aston Martin Vantage AMR GT3", "McLaren 720S GT3 Evo", "Mercedes-AMG GT3", "Ford Mustang GT3", "Lexus RC F GT3" };
        private readonly Random _random;
        private readonly List<DemoCar> _cars = new();
        private readonly double _raceDuration;
        private double _time;
        private double _lastDt;
        private double _wetness;

        public DemoRaceSimulator(int seed = 7, double raceDurationSeconds = 3600)
        {
            _random = new Random(seed);
            _raceDuration = raceDurationSeconds;
            string[] hyper = { "Léa Martin", "Hugo Bernard", "Nina Rossi", "Tom Becker", "Sara Lindqvist", "Yuki Tanaka", "Marco Silva", "Elena Petrova" };
            string[] gt = { "Vous", "Lucas Moreau", "Jade Laurent", "Felix Wagner", "Inès Duarte", "Oscar Novak", "Chloé Girard", "Ravi Kapoor" };
            int number = 2;
            for (int i = 0; i < hyper.Length; i++)
            {
                _cars.Add(new DemoCar(hyper[i], number++, "Hypercar", "HY", HyperCars[i % HyperCars.Length], 108.5 + (i * 0.35) + (_random.NextDouble() * 0.4), 2.9, 0.035, i % 3 == 0 ? "Soft" : "Medium", _random.Next(9, 13)));
            }

            number = 50;
            for (int i = 0; i < gt.Length; i++)
            {
                _cars.Add(new DemoCar(gt[i], number++, "GT3", "GT", Gt3Cars[i % Gt3Cars.Length], 121.0 + (i * 0.3) + (_random.NextDouble() * 0.4), 3.2, 0.04, i % 2 == 0 ? "Medium" : "Soft", _random.Next(11, 15))
                {
                    IsPlayer = i == 0,
                });
            }

            // Grille : départ en file, Hypercars devant.
            for (int i = 0; i < _cars.Count; i++)
            {
                _cars[i].Distance = -i * 12.0;
                _cars[i].LineBias = (_random.NextDouble() - 0.5) * 3.0;
                _cars[i].CornerSkill = (_random.NextDouble() - 0.5) * 0.04;
            }
        }

        public double SessionTime => _time;

        /// <summary>Fait avancer la course de dt secondes (temps de jeu).</summary>
        public SimulatorDataSet Step(double dt)
        {
            _time += dt;
            _lastDt = dt;
            foreach (DemoCar car in _cars)
            {
                Advance(car, dt);
            }

            return Build();
        }

        private void Advance(DemoCar car, double dt)
        {
            if (car.Finished)
            {
                return;
            }

            if (car.PitTimeLeft > 0)
            {
                car.PitTimeLeft -= dt;
                double pitSpeed = 80 / 3.6;
                car.Distance += pitSpeed * dt;
                car.CurrentLapTime += dt;
                if (car.PitTimeLeft <= 0)
                {
                    car.InPits = false;
                    car.Fuel = car.FuelToFill;
                    car.Energy = 100;
                    car.Wear = 0;
                    car.Compound = car.NextCompound;
                    car.WearPerLap = car.Compound == "Soft" ? 0.045 : 0.034;
                    car.PitRequested = false;
                }

                CheckLine(car);
                return;
            }

            double lapPosNow = Mod(car.Distance, TrackLength);
            double lapTarget = car.BaseLap + (car.Wear * 100 * car.DegPerWearPct) + (car.Fuel * 0.03) + car.LapNoise + (Rain() * 6);
            double cornerness = Profile.Cornerness(lapPosNow);
            double speed = Profile.SpeedAt(lapPosNow) * (Profile.LapTime / lapTarget) * (1 + (car.CornerSkill * cornerness));
            car.PreviousSpeed = car.Speed;
            car.Speed = speed;
            car.Distance += speed * dt;
            car.CurrentLapTime += dt;
            car.Fuel = Math.Max(0, car.Fuel - (car.FuelPerLap * speed * dt / TrackLength));
            car.Energy = Math.Max(0, car.Energy - (car.EnergyPerLap * speed * dt / TrackLength));
            car.Wear = Math.Min(1, car.Wear + (car.WearPerLap * speed * dt / TrackLength));

            // Décision d'arrêt : on rentre quand il reste moins d'un tour et demi de carburant/énergie ou pneus trop usés.
            bool needStop = car.Fuel < car.FuelPerLap * 1.5 || car.Energy < car.EnergyPerLap * 1.5 || car.Wear > 0.62;
            double remainingTime = _raceDuration - _time;
            bool worthIt = remainingTime > car.BaseLap * 2;
            if (needStop && worthIt)
            {
                car.PitRequested = true;
            }

            double lapPos = Mod(car.Distance, TrackLength);
            if (car.PitRequested && lapPos > TrackLength - 150 && !car.InPits)
            {
                car.InPits = true;
                double lapsLeft = Math.Ceiling(remainingTime / car.BaseLap) + 1;
                car.FuelToFill = Math.Min(car.Capacity, (lapsLeft * car.FuelPerLap) + 2);
                car.NextCompound = car.Compound == "Soft" ? "Medium" : (_random.NextDouble() < 0.5 ? "Soft" : "Medium");
                car.PitTimeLeft = PitLaneTime + (_random.NextDouble() * 4);
            }

            CheckLine(car);
        }

        private void CheckLine(DemoCar car)
        {
            int laps = car.Distance <= 0 ? 0 : (int)Math.Floor(car.Distance / TrackLength);
            if (laps <= car.CompletedLaps)
            {
                return;
            }

            // Instant exact du passage de ligne (le pas de simulation peut être long en démo accélérée).
            double overshoot = car.Distance - (laps * TrackLength);
            double crossing = _time - (car.Speed > 1 ? Math.Clamp(overshoot / car.Speed, 0, _lastDt) : 0);
            if (car.Distance > 0 && car.CompletedLaps >= 0 && car.LapStartTime.HasValue)
            {
                double lapTime = crossing - car.LapStartTime.Value;
                car.LastLap = lapTime;
                car.Sectors = new[] { lapTime * 0.31, lapTime * 0.36, lapTime * 0.33 };
                if (car.BestLap <= 0 || lapTime < car.BestLap)
                {
                    car.BestLap = lapTime;
                }
            }

            car.CompletedLaps = laps;
            car.LapStartTime = crossing;
            car.CurrentLapTime = _time - crossing;
            car.LapNoise = (_random.NextDouble() - 0.4) * 0.9;
            if (_time >= _raceDuration && car.Distance >= LeaderDistance())
            {
                car.Finished = true;
            }
            else if (_time >= _raceDuration && _cars.Any(c => c.Finished))
            {
                car.Finished = true;
            }
        }

        private double LeaderDistance() => _cars.Max(c => c.Distance);

        private SimulatorDataSet Build()
        {
            var ordered = _cars.OrderByDescending(c => c.Distance).ToList();
            var data = new SimulatorDataSet("Demo")
            {
                SimulatorSourceInfo = new SimulatorSourceInfo
                {
                    HasLapTimeInformation = true,
                    SectorTimingSupport = DataInputSupport.Full,
                    GapInformationProvided = GapInformationKind.None,
                    GlobalTyreCompounds = true,
                },
            };

            SessionInfo s = data.SessionInfo;
            s.SessionTime = TimeSpan.FromSeconds(_time);
            // SMHUB_DEMO_SESSION=essais|qualif : même plateau, en essais ou qualification (pour vérifier l'affichage hors course).
            s.SessionType = Environment.GetEnvironmentVariable("SMHUB_DEMO_SESSION") switch { "essais" => SessionType.Practice, "qualif" => SessionType.Qualification, _ => SessionType.Race };
            s.SessionPhase = _cars.Any(c => c.Finished) ? SessionPhase.Checkered : SessionPhase.Green;
            s.SessionLengthType = SessionLengthType.Time;
            s.SessionTimeRemaining = Math.Max(0, _raceDuration - _time);
            s.IsActive = true;
            s.IsMultiClass = true;
            s.TrackInfo = new TrackInfo { TrackName = "Circuit de démonstration", TrackLayoutName = "GP", LayoutLength = Distance.FromMeters(TrackLength) };
            double rain = Rain();
            _wetness += ((rain * 100 * 1.2) - _wetness) * Math.Min(1, 0.004 * _lastDt);
            _wetness = Math.Clamp(_wetness, 0, 100);
            s.WeatherInfo = new WeatherInfo
            {
                AirTemperature = Temperature.FromCelsius(23.5 + (1.5 * Math.Sin(_time / 5400 * Math.PI)) - (rain * 3)),
                TrackTemperature = Temperature.FromCelsius(TrackTemp()),
                RainIntensity = (int)Math.Round(rain * 100),
                TrackWetness = (int)Math.Round(_wetness),
                HasWindInformation = true,
                WindSpeed = Velocity.FromKph(12 + (6 * Math.Sin(_time / 900))),
                WindDirectionFrom = Mod(240 + (20 * Math.Sin(_time / 1300)), 360),
            };
            s.LeaderCurrentLap = ordered[0].CompletedLaps + 1;

            var drivers = new List<DriverInfo>();
            var classCounters = new Dictionary<string, int>();
            for (int i = 0; i < ordered.Count; i++)
            {
                DemoCar c = ordered[i];
                classCounters[c.ClassId] = classCounters.TryGetValue(c.ClassId, out int n) ? n + 1 : 1;
                double lapDistance = Mod(Math.Max(0, c.Distance), TrackLength);
                var (x, z) = Profile.Position(lapDistance, c.InPits ? -9 : Profile.RacingLineOffset(lapDistance, c.LineBias));
                var d = new DriverInfo
                {
                    DriverSessionId = "demo-" + c.Number,
                    DriverLongName = c.Name,
                    DriverShortName = c.Name == "Vous" ? "VOU" : c.Name.Split(' ')[^1][..3].ToUpperInvariant(),
                    CarName = c.Car,
                    CarClassName = c.ClassName,
                    CarClassId = c.ClassId,
                    TeamName = "Équipe " + c.Number,
                    CarRaceNumber = c.Number,
                    IsCarRaceNumberFilled = true,
                    CompletedLaps = Math.Max(0, c.CompletedLaps),
                    InPits = c.InPits,
                    PitStopRequested = c.PitRequested,
                    IsPlayer = c.IsPlayer,
                    Position = i + 1,
                    PositionInClass = classCounters[c.ClassId],
                    CurrentLapValid = true,
                    LapDistance = lapDistance,
                    TotalDistance = Math.Max(0, c.Distance),
                    FinishStatus = c.Finished ? DriverFinishStatus.Finished : DriverFinishStatus.None,
                    WorldPosition = new Point3D(Distance.FromMeters(x), Distance.FromMeters(0), Distance.FromMeters(z)),
                    Speed = Velocity.FromMs(c.InPits ? 80 / 3.6 : c.Speed),
                };

                d.Timing.LastLapTime = TimeSpan.FromSeconds(c.LastLap);
                d.Timing.BestLapTime = TimeSpan.FromSeconds(c.BestLap);
                d.Timing.CurrentLapTime = TimeSpan.FromSeconds(c.CurrentLapTime);
                d.Timing.CurrentSector = lapDistance < TrackLength * 0.31 ? 1 : (lapDistance < TrackLength * 0.67 ? 2 : 3);
                if (c.Sectors != null)
                {
                    d.Timing.LastSector1Time = TimeSpan.FromSeconds(c.Sectors[0]);
                    d.Timing.LastSector2Time = TimeSpan.FromSeconds(c.Sectors[1]);
                    d.Timing.LastSector3Time = TimeSpan.FromSeconds(c.Sectors[2]);
                }

                foreach (WheelInfo w in new[] { d.CarInfo.WheelsInfo.FrontLeft, d.CarInfo.WheelsInfo.FrontRight, d.CarInfo.WheelsInfo.RearLeft, d.CarInfo.WheelsInfo.RearRight })
                {
                    w.TyreType = c.Compound;
                }

                if (c.IsPlayer)
                {
                    FillPlayer(d, c);
                    data.InputInfo = _input;
                }

                drivers.Add(d);
            }

            data.DriversInfo = drivers.ToArray();
            data.PlayerInfo = drivers.First(d => d.IsPlayer);
            data.LeaderInfo = drivers[0];
            return data;
        }

        private void FillPlayer(DriverInfo d, DemoCar c)
        {
            FuelInfo fuel = d.CarInfo.FuelSystemInfo;
            fuel.FuelCapacity = Volume.FromLiters(c.Capacity);
            fuel.FuelRemaining = Volume.FromLiters(c.Fuel);
            fuel.HasVirtualTank = true;
            fuel.VirtualTank = Percentage.FromPercentage(c.Energy);

            double dt = Math.Max(0.01, _lastDt);
            double lapPos = Mod(Math.Max(0, c.Distance), TrackLength);
            double v = c.InPits ? 80 / 3.6 : c.Speed;
            double curvature = Profile.CurvatureAt(lapPos);
            double latAcc = v * v * Math.Abs(curvature);
            double decel = Math.Max(0, (c.PreviousSpeed - c.Speed) / dt);
            double track = TrackTemp();
            c.Thermal ??= new DemoThermal();
            DemoThermal th = c.Thermal;

            var wheels = new[] { d.CarInfo.WheelsInfo.FrontLeft, d.CarInfo.WheelsInfo.FrontRight, d.CarInfo.WheelsInfo.RearLeft, d.CarInfo.WheelsInfo.RearRight };
            double[] wearFactor = { 1.08, 0.95, 1.0, 0.9 };
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                bool left = i % 2 == 0;
                // Virage à gauche (courbure > 0) : les roues droites travaillent davantage.
                double side = (curvature > 0 && !left) || (curvature < 0 && left) ? 1.35 : 0.7;
                double target = 70 + (latAcc * 0.9 * side) + (c.Wear * 12) + ((track - 30) * 0.5) + (front ? 3 : 0) + (decel * (front ? 0.6 : 0.25)) - (Rain() * 25);
                th.Surface[i] += (target - th.Surface[i]) * Math.Min(1, dt * 0.9);
                th.Core[i] += (th.Surface[i] + 6 - th.Core[i]) * Math.Min(1, dt * 0.04);
                double heat = decel * (front ? 9 : 6);
                th.Brake[i] += (heat - ((th.Brake[i] - 120) * 0.075)) * dt;
                th.Brake[i] = Math.Clamp(th.Brake[i], 80, 1000);

                WheelInfo w = wheels[i];
                w.TyreWear.ActualWear = Math.Min(1, c.Wear * wearFactor[i]);
                w.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(160 + ((th.Core[i] - 70) * 0.45) + (i * 0.6));
                w.TyrePressure.IdealQuantity = Pressure.FromKiloPascals(175);
                w.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(th.Surface[i] + (left ? -2 : 4)); // intérieur plus chaud (carrossage)
                w.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(th.Surface[i]);
                w.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(th.Surface[i] + (left ? 4 : -2));
                w.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
                w.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(10);
                w.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(th.Core[i]);
                w.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(th.Brake[i]);
            }

            double load = c.InPits ? 0.2 : Math.Clamp(v / 85, 0, 1);
            th.Water += (85 + (load * 9) - th.Water) * Math.Min(1, dt * 0.02);
            th.Oil += (98 + (load * 16) - th.Oil) * Math.Min(1, dt * 0.012);
            d.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(th.Water);
            d.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(th.Oil);
            d.CarInfo.OilSystemInfo.OilPressure = Pressure.FromKiloPascals(280 + (load * 220));
            d.CarInfo.EngineRpm = c.InPits ? 3500 : (int)(4500 + (load * 4000));
            d.CarInfo.CurrentGear = c.InPits ? "2" : Math.Clamp((int)(v / 13) + 1, 1, 7).ToString();

            // Pédales, volant et réglages en roulant (comme une hypercar de LMU) pour tester l'analyse du pilotage sans jeu.
            double accel = (c.Speed - c.PreviousSpeed) / dt;
            _input = new SecondMonitor.DataModel.BasicProperties.InputInfo
            {
                ThrottlePedalPosition = c.InPits ? 0.2 : decel > 1 ? 0 : Math.Clamp(0.35 + (accel * 0.12) + (Math.Abs(curvature) < 0.002 ? 0.65 : 0), 0, 1),
                BrakePedalPosition = c.InPits ? 0 : Math.Clamp(decel / 25, 0, 1),
                SteeringInput = Math.Clamp(curvature * 40, -1, 1),
            };
            CarControls controls = d.CarInfo.Controls;
            controls.Available = true;
            controls.FrontBrakeBiasPct = 54.5;
            controls.Migration = 3;
            controls.MigrationMax = 6;
            controls.Tc = 4;
            controls.TcMax = 11;
            controls.TcCut = 3;
            controls.TcCutMax = 11;
            controls.MotorMap = 2;
            controls.MotorMapMax = 4;
            controls.RegenKw = 200;
            controls.ElectricMotorState = c.InPits ? 1 : decel > 1 ? 3 : v > 52 ? 2 : 1;
            controls.BatteryPct = 60;
            controls.VirtualEnergyPct = Math.Round(c.Energy, 1);
            controls.Ignition = 2;
            controls.TrackLimitsPoints = 0;
            controls.TrackLimitsPointsPerPenalty = 3;
            controls.Penalties = 0;
        }

        private SecondMonitor.DataModel.BasicProperties.InputInfo _input = new();

        /// <summary>Averse de démonstration entre la 35e et la 52e minute (intensité 0..1).</summary>
        private double Rain()
        {
            double m = _time / 60;
            return m < 35 || m > 52 ? 0 : 0.35 * Math.Sin((m - 35) / 17 * Math.PI);
        }

        private double TrackTemp() => 31.2 + (5 * Math.Sin(_time / 4800 * Math.PI)) - (Rain() * 9) - (_wetness * 0.05);

        private static double Mod(double a, double b) => ((a % b) + b) % b;

        private sealed class DemoCar
        {
            public DemoCar(string name, int number, string className, string classId, string car, double baseLap, double fuelPerLap, double degPerWearPct, string compound, int stintTarget)
            {
                Name = name;
                Number = number;
                ClassName = className;
                ClassId = classId;
                Car = car;
                BaseLap = baseLap;
                FuelPerLap = fuelPerLap;
                DegPerWearPct = degPerWearPct;
                Compound = compound;
                Capacity = Math.Round(fuelPerLap * stintTarget, 0);
                Fuel = Capacity;
                EnergyPerLap = 100.0 / (stintTarget + 0.6);
                WearPerLap = compound == "Soft" ? 0.045 : 0.034;
            }

            public string Name { get; }

            public int Number { get; }

            public string ClassName { get; }

            public string ClassId { get; }

            public string Car { get; }

            public double BaseLap { get; }

            public double FuelPerLap { get; }

            public double EnergyPerLap { get; }

            public double DegPerWearPct { get; }

            public double WearPerLap { get; set; }

            public double Capacity { get; }

            public bool IsPlayer { get; set; }

            public string Compound { get; set; }

            public string NextCompound { get; set; }

            public double Distance { get; set; }

            public double Speed { get; set; }

            public double PreviousSpeed { get; set; }

            public double LineBias { get; set; }

            public double CornerSkill { get; set; }

            public DemoThermal Thermal { get; set; }

            public double Fuel { get; set; }

            public double FuelToFill { get; set; }

            public double Energy { get; set; } = 100;

            public double Wear { get; set; }

            public bool InPits { get; set; }

            public bool PitRequested { get; set; }

            public double PitTimeLeft { get; set; }

            public int CompletedLaps { get; set; }

            public double? LapStartTime { get; set; }

            public double CurrentLapTime { get; set; }

            public double LastLap { get; set; }

            public double BestLap { get; set; }

            public double[] Sectors { get; set; }

            public double LapNoise { get; set; }

            public bool Finished { get; set; }
        }
    }
}

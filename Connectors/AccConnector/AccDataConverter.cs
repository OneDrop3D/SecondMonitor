namespace SecondMonitor.AccConnector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using Foundation.Connectors;
    using NLog;
    using Repository;
    using SharedMemory;
    using UDPData;
    using UDPData.Structs;
    using CarInfo = DataModel.Snapshot.Systems.CarInfo;
    using DriverInfo = DataModel.Snapshot.Drivers.DriverInfo;
    using SessionPhase = DataModel.Snapshot.SessionPhase;

    public class AccDataConverter : AbstractDataConvertor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan StaleCarDataTime = TimeSpan.FromSeconds(5);

        private readonly string _customPlayerName;
        private readonly AccSettingsRepository _settingsRepository;
        private readonly Stopwatch _brakePadsStopwatch;

        private readonly Dictionary<string, TimeSpan[]> _currentSectorTimes;
        private readonly Stopwatch _sessionTimeWatch;
        private readonly Dictionary<string, Stopwatch> _pitDurationMap;
        private SPageFilePhysics _lastValidPhysics;

        private DriverInfo _lastPlayer;
        private int _playerLastSector;
        private SectorDistances _sectorDistances;

        private bool _isAccSharedMemoryActive;
        private int _lastSharedMemoryPacketId;
        private DateTime _lastAccSharedMemoryUpdate;

        public AccDataConverter(string customPlayerName, AccSettingsRepository settingsRepository)
        {
            _customPlayerName = customPlayerName;
            _settingsRepository = settingsRepository;
            _sessionTimeWatch = Stopwatch.StartNew();
            _currentSectorTimes = new Dictionary<string, TimeSpan[]>();
            _brakePadsStopwatch = Stopwatch.StartNew();
            _pitDurationMap = new Dictionary<string, Stopwatch>();
        }

        public void Reset()
        {
            _currentSectorTimes.Clear();
            _sessionTimeWatch.Start();
            _pitDurationMap.Clear();
            _playerLastSector = -1;
            _sectorDistances = null;
        }

        public SimulatorDataSet CreateSimulatorDataSet(AccShared accData, FullUdpData fullUdpData)
        {
            SimulatorDataSet simData = new SimulatorDataSet(SimulatorsNameMap.AccSimName)
            {
                SimulatorSourceInfo =
                {
                    HasLapTimeInformation = true,
                    OutLapIsValid = false,
                    SimNotReportingEndOfOutLapCorrectly = true,
                    ForceLapOverTime = true,
                    GlobalTyreCompounds = false,
                    SectorTimingSupport = _sectorDistances?.HasValidDistances == true ? DataInputSupport.Full : DataInputSupport.PlayerOnly,
                    TelemetryInfo = { ContainsSuspensionTravel = true, ContainsWheelRps = true, ContainsTyreLoad = true },
                    WorldPositionInvalid = accData.AcsGraphic.status == AcStatus.AcOffSpectate,
                }
            };

            if (_lastValidPhysics.waterTemp > 0 && accData.AcsPhysics.waterTemp <= 0)
            {
                accData.AcsPhysics = _lastValidPhysics;
            }
            else
            {
                _lastValidPhysics = accData.AcsPhysics;
            }

            CheckAccSharedMemory(accData.AcsGraphic);

            /*if ((!fullUdpData.CarsDictionary.TryGetValue(accData.AcsGraphic.playerCarID, out FullCarData dummy)) && _lastValidDataSet != null )
            {
                return _lastValidDataSet;
            }*/

            FillSessionInfo(accData, simData, fullUdpData);
            FillPitWindowInformation(accData, simData, fullUdpData);
            AddDriversData(simData, accData, fullUdpData);

            FillPlayerCarInfo(accData, simData);

            // PEDAL INFO
            AddPedalInfo(accData, simData);

            // WaterSystemInfo
            AddWaterSystemInfo(simData, accData);

            // OilSystemInfo
            AddOilSystemInfo(simData, accData);

            // Brakes Info
            AddBrakesInfo(accData, simData);

            // Tyre Pressure Info
            AddTyresAndFuelInfo(simData, accData);

            // Acceleration
            AddAcceleration(simData, accData);

            //Add Additional Player Car Info
            AddPlayerCarInfo(accData, simData);

            PopulateClassPositions(simData);

            FillSpectateInfo(accData, simData);

            return simData;
        }

        private static void FillPlayersGear(AccShared accData, SimulatorDataSet simData)
        {
            int gear = accData.AcsPhysics.gear - 1;
            simData.PlayerInfo.CarInfo.CurrentGear = gear switch
            {
                0 => "N",
                -1 => "R",
                -2 => string.Empty,
                _ => gear.ToString()
            };
        }

        private static void FillWeatherInformation(AccShared data, SimulatorDataSet simData, FullUdpData fullUdpData)
        {
            WeatherInfo weatherInfo = simData.SessionInfo.WeatherInfo;
            switch (data.AcsGraphic.rainIntensity)
            {
                case AccRainIntensity.Drizzle:
                    weatherInfo.RainIntensity = 10;
                    break;
                case AccRainIntensity.LightRain:
                    weatherInfo.RainIntensity = 25;
                    break;
                case AccRainIntensity.MediumRain:
                    weatherInfo.RainIntensity = 50;
                    break;
                case AccRainIntensity.HeavyRain:
                    weatherInfo.RainIntensity = 75;
                    break;
                case AccRainIntensity.Thunderstorm:
                    weatherInfo.RainIntensity = 100;
                    break;
                case AccRainIntensity.NoRain:
                default:
                    weatherInfo.RainIntensity = 0;
                    break;
            }

            switch (data.AcsGraphic.trackGripStatus)
            {
                case AccTrackGripStatus.Greasy:
                    weatherInfo.TrackWetness = 10;
                    break;
                case AccTrackGripStatus.Damp:
                    weatherInfo.TrackWetness = 25;
                    break;
                case AccTrackGripStatus.Wet:
                    weatherInfo.TrackWetness = 50;
                    break;
                case AccTrackGripStatus.Flooded:
                    weatherInfo.TrackWetness = 100;
                    break;
                case AccTrackGripStatus.Green:
                case AccTrackGripStatus.Fast:
                case AccTrackGripStatus.Optimum:
                default:
                    weatherInfo.TrackWetness = 0;
                    break;
            }

            weatherInfo.AirTemperature = Temperature.FromCelsius(fullUdpData.LastRealtimeUpdate.AmbientTemp);
            weatherInfo.TrackTemperature = Temperature.FromCelsius(fullUdpData.LastRealtimeUpdate.TrackTemp);
            weatherInfo.HasWindInformation = true;
            weatherInfo.WindSpeed = Velocity.FromKph(data.AcsGraphic.windSpeed);
            weatherInfo.WindDirectionFrom = data.AcsGraphic.windDirection;
        }

        private static void AddAcceleration(SimulatorDataSet simData, AccShared accData)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinMs = accData.AcsPhysics.accG[0] * 9.8;
            simData.PlayerInfo.CarInfo.Acceleration.YinMs = accData.AcsPhysics.accG[1] * 9.8;
            simData.PlayerInfo.CarInfo.Acceleration.ZinMs = accData.AcsPhysics.accG[2] * 9.8;
        }

        private static void AddBrakesInfo(AccShared accData, SimulatorDataSet simData)
        {
            Temperature idealFrontTemp;
            Temperature idealFrontTempWindow;

            double max;
            double min;

            /*
             * The values here come from https://github.com/notsissyHex/ACCBoP and seems to be pretty accurate.
             * It should ultimately help better understand what the actual ranges are. The only compound missing is
             * compound #4, but it will be rare for people to actually use these in a race due to it being a "test
             * compound" made by Kunos to easily test brake pad wear.
             */
            switch (accData.AcsPhysics.frontBrakeCompound)
            {
                // Brake Compound 1
                case 0:
                    max = 500.0;
                    min = 250.0;
                    idealFrontTemp = Temperature.FromCelsius((min + max) / 2);
                    idealFrontTempWindow = Temperature.FromCelsius((max - min) / 2);
                    break;
                // Brake Compound 2
                case 1:
                    max = 600.0;
                    min = 250.0;
                    idealFrontTemp = Temperature.FromCelsius((min + max) / 2);
                    idealFrontTempWindow = Temperature.FromCelsius((max - min) / 2);
                    break;
                // Brake Compound 3
                case 2:
                    max = 800.0;
                    min = 200.0;
                    idealFrontTemp = Temperature.FromCelsius((min + max) / 2);
                    idealFrontTempWindow = Temperature.FromCelsius((max - min) / 2);
                    break;
                // Brake Compound 4
                default:
                    idealFrontTemp = WheelInfo.IdealBrakeTemperature;
                    idealFrontTempWindow = WheelInfo.IdealBrakeTemperatureWindow;
                    break;
            }

            Temperature idealRearTemp;
            Temperature idealRearTempWindow;

            switch (accData.AcsPhysics.frontBrakeCompound)
            {
                // Brake Compound 1
                case 0:
                    max = 600.0;
                    min = 150.0;
                    idealRearTemp = Temperature.FromCelsius((min + max) / 2);
                    idealRearTempWindow = Temperature.FromCelsius((max - min) / 2);
                    break;
                // Brake Compound 2
                case 1:
                    max = 600.0;
                    min = 150.0;
                    idealRearTemp = Temperature.FromCelsius((min + max) / 2);
                    idealRearTempWindow = Temperature.FromCelsius((max - min) / 2);
                    break;
                // Brake Compound 3
                case 2:
                    max = 700.0;
                    min = 130.0;
                    idealRearTemp = Temperature.FromCelsius((min + max) / 2);
                    idealRearTempWindow = Temperature.FromCelsius((max - min) / 2);
                    break;
                // Brake Compound 4
                default:
                    idealRearTemp = WheelInfo.IdealBrakeTemperature;
                    idealRearTempWindow = WheelInfo.IdealBrakeTemperatureWindow;
                    break;
            }

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.IdealQuantity = idealFrontTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.IdealQuantityWindow = idealFrontTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.brakeTemp[(int)AcWheels.FL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakesDamage.Damage = CalculateBrakeDamageInfo(accData.AcsPhysics.padLife[(int)AcWheels.FL], accData.AcsPhysics.discLifeLife[(int)AcWheels.FL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.IdealQuantity = idealFrontTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.IdealQuantityWindow = idealFrontTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.brakeTemp[(int)AcWheels.FR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakesDamage.Damage = CalculateBrakeDamageInfo(accData.AcsPhysics.padLife[(int)AcWheels.FR], accData.AcsPhysics.discLifeLife[(int)AcWheels.FR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.IdealQuantity = idealRearTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.IdealQuantityWindow = idealRearTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.brakeTemp[(int)AcWheels.RL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakesDamage.Damage = CalculateBrakeDamageInfo(accData.AcsPhysics.padLife[(int)AcWheels.RL], accData.AcsPhysics.discLifeLife[(int)AcWheels.RL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.IdealQuantity = idealRearTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.IdealQuantityWindow = idealRearTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.brakeTemp[(int)AcWheels.RR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakesDamage.Damage = CalculateBrakeDamageInfo(accData.AcsPhysics.padLife[(int)AcWheels.RR], accData.AcsPhysics.discLifeLife[(int)AcWheels.RR]);
        }

        private static double CalculateBrakeDamageInfo(double padLife, double discLife)
        {
            // Calculating brake damage as the thickness in millimeters with "finished" brakes at 5 millimeters. Most likely not exact, but a good approximation for now.
            const double padThicknessInMillimeters = 29;
            const double discThicknessInMillimeters = 32;
            const double padLifeBuffer = 5;

            var padLifeUsed = padThicknessInMillimeters - padLife;
            var padDamage = Math.Min(1.0, padLifeUsed / (padThicknessInMillimeters - padLifeBuffer));

            var discLifeUsed = discThicknessInMillimeters - discLife;
            var discDamage = Math.Min(1.0, discLifeUsed / (discThicknessInMillimeters - padLifeBuffer));

            return Math.Max(padDamage, discDamage);
        }

        private static void AddOilSystemInfo(SimulatorDataSet simData, AccShared accData)
        {
            simData.PlayerInfo.CarInfo.TurboPressure = Pressure.FromAtm(accData.AcsPhysics.turboBoost);
            simData.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.waterTemp);
        }

        private static void AddWaterSystemInfo(SimulatorDataSet simData, AccShared data)
        {
            simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(data.AcsPhysics.waterTemp);
        }

        private static void AddPedalInfo(AccShared accData, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = accData.AcsPhysics.gas;
            simData.InputInfo.BrakePedalPosition = accData.AcsPhysics.brake;
            simData.InputInfo.ClutchPedalPosition = 1 - accData.AcsPhysics.clutch;
            simData.InputInfo.SteeringInput = accData.AcsPhysics.steerAngle;
        }

        private void FillSpectateInfo(AccShared accData, SimulatorDataSet simData)
        {
            simData.SessionInfo.SpectatingState = accData.AcsGraphic.status == AcStatus.AcOffSpectate ? SpectatingState.Spectate : SpectatingState.Live;
        }

        private void AddPlayerCarInfo(AccShared data, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;

            /*
             Damage Thresholds are based off what CrewChief uses, with CrewChief taking the approach of
             comparing the sum of the first four elements and comparing them with the damage thresholds
             below.
             */
            playerCar.CarDamageInformation.Bodywork.MediumDamageThreshold = 40.0 / 1000.0;
            playerCar.CarDamageInformation.Bodywork.HeavyDamageThreshold = 130.0 / 1000.0;
            // carDamage array seems to sum damage into index 4. Max will always grab this value.
            playerCar.CarDamageInformation.Bodywork.Damage = data.AcsPhysics.carDamage[4] / 1000.0;

            playerCar.CarDamageInformation.Suspension.MediumDamageThreshold = 0.08;
            playerCar.CarDamageInformation.Suspension.HeavyDamageThreshold = 0.25;
            playerCar.CarDamageInformation.Suspension.Damage = data.AcsPhysics.suspensionDamage.Max();

            // Détail (SecondMonitor multiplateforme) : carrosserie par zone (avant, arrière, gauche, droite) et suspension par roue.
            var parts = playerCar.CarDamageInformation.Parts;
            parts.Clear();
            string[] zones = { "body-front", "body-rear", "body-left", "body-right" };
            for (int i = 0; i < 4 && i < data.AcsPhysics.carDamage.Length; i++)
            {
                parts.Add(new DamagePart(zones[i], "aero", Math.Min(1, data.AcsPhysics.carDamage[i] / 1000.0)));
            }

            string[] accWheels = { "fl", "fr", "rl", "rr" };
            for (int i = 0; i < 4 && i < data.AcsPhysics.suspensionDamage.Length; i++)
            {
                parts.Add(new DamagePart($"suspension-{accWheels[i]}", "suspension", Math.Min(1, data.AcsPhysics.suspensionDamage[i])));
            }

            simData.PlayerInfo.CarInfo.SpeedLimiterEngaged = data.AcsPhysics.pitLimiterOn == 1;
            if (data.AcsGraphic.driverStintTimeLeft > 0)
            {
                simData.PlayerInfo.StintTimeLeft = TimeSpan.FromMilliseconds(Math.Min(data.AcsGraphic.driverStintTimeLeft, data.AcsGraphic.driverStintTotalTimeLeft));
            }

            playerCar.WorldOrientation = new Orientation()
            {
                Pitch = Angle.GetFromRadians(data.AcsPhysics.pitch),
                Yaw = Angle.GetFromRadians(data.AcsPhysics.heading),
                Roll = Angle.GetFromRadians(data.AcsPhysics.roll),
            };
        }

        private void CheckAccSharedMemory(SPageFileGraphic accDataAcsGraphic)
        {
            if (accDataAcsGraphic.packetId != _lastSharedMemoryPacketId)
            {
                _lastSharedMemoryPacketId = accDataAcsGraphic.packetId;
                _lastAccSharedMemoryUpdate = DateTime.Now;
                _isAccSharedMemoryActive = true;
                return;
            }

            if ((DateTime.Now - _lastAccSharedMemoryUpdate).TotalSeconds > 5)
            {
                _isAccSharedMemoryActive = false;
            }
        }

        private void FillPlayerCarInfo(AccShared accData, SimulatorDataSet simData)
        {
            FillPlayersGear(accData, simData);
            switch (accData.AcsGraphic.lightsStage)
            {
                case 1:
                    simData.PlayerInfo.CarInfo.HeadLightsStatus = HeadLightsStatus.NormalBeams;
                    break;
                case 2:
                    simData.PlayerInfo.CarInfo.HeadLightsStatus = HeadLightsStatus.HighBeams;
                    break;
                default:
                    simData.PlayerInfo.CarInfo.HeadLightsStatus = HeadLightsStatus.Off;
                    break;
            }

            simData.PlayerInfo.CarInfo.EngineRpm = accData.AcsPhysics.rpms;
            FillSafetySystems(accData, simData);
        }

        private void FillSafetySystems(AccShared accData, SimulatorDataSet simData)
        {
            if (simData.PlayerInfo == null)
            {
                return;
            }

            var playerCar = simData.PlayerInfo.CarInfo;
            playerCar.AbsInfo.IsAvailable = accData.AcsGraphic.ABS > 0;
            playerCar.AbsInfo.Remark = accData.AcsGraphic.ABS.ToString();
            playerCar.AbsInfo.IsActive = accData.AcsPhysics.abs > 0;

            playerCar.TcInfo.IsAvailable = accData.AcsGraphic.TC > 0 || accData.AcsGraphic.TCCut > 0;
            playerCar.TcInfo.Remark = accData.AcsGraphic.TCCut > 0 ? $"{accData.AcsGraphic.TC.ToString()} / {accData.AcsGraphic.TCCut.ToString()}" : accData.AcsGraphic.TC.ToString(); 
            playerCar.TcInfo.IsActive = accData.AcsPhysics.tc > 0;

            // Réglages en roulant (SecondMonitor multiplateforme). La répartition de freinage d'ACC dépend d'un décalage
            // propre à chaque voiture : elle n'est pas donnée, pour ne pas annoncer un chiffre faux.
            CarControls controls = playerCar.Controls;
            controls.Available = true;
            controls.Tc = accData.AcsGraphic.TC;
            controls.TcCut = accData.AcsGraphic.TCCut;
            controls.Abs = accData.AcsGraphic.ABS;
            controls.MotorMap = accData.AcsGraphic.EngineMap + 1; // ACC compte à partir de 0, l'écran de la voiture à partir de 1
            controls.SpeedLimiterAvailable = true;
        }

        internal void FillSessionInfo(AccShared data, SimulatorDataSet simData, FullUdpData fullUdpData)
        {
            // Timing
            //simData.SessionInfo.SessionTime = fullUdpData.LastRealtimeUpdate.SessionTime < TimeSpan.Zero ? _sessionTimeWatch.Elapsed : fullUdpData.LastRealtimeUpdate.SessionTime;
            simData.SessionInfo.SessionTime = _sessionTimeWatch.Elapsed;
            simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(fullUdpData.CurrentTrack.TrackMeters);
            simData.SessionInfo.TrackInfo.TrackName = fullUdpData.CurrentTrack.TrackName;
            simData.SessionInfo.TrackInfo.TrackLayoutName = string.Empty;
            FillWeatherInformation(data, simData, fullUdpData);
            FillFlags(simData.SessionInfo, data.AcsGraphic);
            //simData.SessionInfo.WeatherInfo.WindDirectionFrom = (data.AcsGraphic.windDirection + 180) % 360;
            simData.SessionInfo.IsMultiplayer = data.AcsStatic.isOnline == 1 || data.AcsGraphic.carCount == 0;

            if (fullUdpData.TimeSinceLastUpdate.Seconds > 5 && (data.AcsGraphic.status == AcStatus.AcOffSpectate || data.AcsGraphic.carCount == 0))
            {
                simData.SessionInfo.SessionType = SessionType.Na;
                simData.SessionInfo.SessionPhase = SessionPhase.Unavailable;
                return;
            }

            switch (fullUdpData.LastRealtimeUpdate.SessionType)
            {
                case RaceSessionType.Practice:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
                case RaceSessionType.Qualifying:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case RaceSessionType.Superpole:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case RaceSessionType.Race:
                    simData.SessionInfo.SessionType = SessionType.Race;
                    break;
                case RaceSessionType.Hotlap:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
                case RaceSessionType.Hotstint:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
                case RaceSessionType.HotlapSuperpole:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case RaceSessionType.Replay:
                default:
                    simData.SessionInfo.SessionType = SessionType.Na;
                    break;
            }

            switch (fullUdpData.LastRealtimeUpdate.Phase)
            {
                case UDPData.SessionPhase.NONE:
                    simData.SessionInfo.SessionPhase = SessionPhase.Unavailable;
                    break;
                case UDPData.SessionPhase.Starting:
                case UDPData.SessionPhase.PreFormation:
                case UDPData.SessionPhase.FormationLap:
                case UDPData.SessionPhase.PreSession:
                    simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                    break;
                case UDPData.SessionPhase.Session:
                case UDPData.SessionPhase.SessionOver:
                    simData.SessionInfo.SessionPhase = SessionPhase.Green;
                    break;
                case UDPData.SessionPhase.PostSession:
                case UDPData.SessionPhase.ResultUI:
                    simData.SessionInfo.SessionPhase = SessionPhase.Checkered;
                    break;
                default:
                    simData.SessionInfo.SessionPhase = SessionPhase.Unavailable;
                    break;
            }

            simData.SessionInfo.IsActive = data.AcsGraphic.status == AcStatus.AcLive || (data.AcsGraphic.status == AcStatus.AcOffSpectate && fullUdpData.TimeSinceLastUpdate.Seconds < 5);

            if (simData.SessionInfo.IsActive && !_sessionTimeWatch.IsRunning)
            {
                _sessionTimeWatch.Start();
            }

            if (!simData.SessionInfo.IsActive && _sessionTimeWatch.IsRunning)
            {
                _sessionTimeWatch.Stop();
            }

            simData.SessionInfo.SessionLengthType = SessionLengthType.Time;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (simData.SessionInfo.SessionType != SessionType.Race && fullUdpData.LastRealtimeUpdate.SessionEndTime.TotalSeconds <= 0)
            {
                simData.SessionInfo.SessionTimeRemaining = 0;
            }
            else
            {
                simData.SessionInfo.SessionTimeRemaining = fullUdpData.LastRealtimeUpdate.SessionEndTime.TotalSeconds;
            }
        }

        private void FillFlags(SessionInfo sessionInfo, SPageFileGraphic sPageFileGraphic)
        {
            if (sPageFileGraphic.GlobalYellow1 == 1)
            {
                sessionInfo.ActiveFlags |= FlagKind.YellowSector1;
            }

            if (sPageFileGraphic.GlobalYellow2 == 1)
            {
                sessionInfo.ActiveFlags |= FlagKind.YellowSector2;
            }

            if (sPageFileGraphic.GlobalYellow3 == 1)
            {
                sessionInfo.ActiveFlags |= FlagKind.YellowSector3;
            }

            if (sPageFileGraphic.GlobalRed == 1)
            {
                sessionInfo.ActiveFlags |= FlagKind.Red;
            }
        }

        private void FillPitWindowInformation(AccShared data, SimulatorDataSet simData, FullUdpData fullUdpData)
        {
            if (data.AcsStatic.PitWindowStart <= 0 || data.AcsStatic.PitWindowStart >= data.AcsStatic.PitWindowEnd || simData.SessionInfo.SessionType != SessionType.Race)
            {
                return;
            }

            TimeSpan timeDifference = simData.SessionInfo.SessionTime - fullUdpData.LastRealtimeUpdate.SessionTime;
            TimeSpan totalHalfSessionTime = TimeSpan.FromMilliseconds((fullUdpData.LastRealtimeUpdate.SessionTime + fullUdpData.LastRealtimeUpdate.SessionEndTime).TotalMilliseconds / 2.0);
            TimeSpan pitWindowHalfDuration = TimeSpan.FromMilliseconds((data.AcsStatic.PitWindowEnd - data.AcsStatic.PitWindowStart) / 2.0);
            TimeSpan pitWindowStart = totalHalfSessionTime - pitWindowHalfDuration;
            TimeSpan pitWindowEnd = totalHalfSessionTime + pitWindowHalfDuration;
            simData.SessionInfo.PitWindow.PitWindowStart = (timeDifference + pitWindowStart).TotalMinutes;
            simData.SessionInfo.PitWindow.PitWindowEnd = (timeDifference + pitWindowEnd).TotalMinutes;

            if (pitWindowStart > fullUdpData.LastRealtimeUpdate.SessionTime)
            {
                simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.BeforePitWindow;
            }
            else if (pitWindowEnd > fullUdpData.LastRealtimeUpdate.SessionTime)
            {
                simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.InPitWindow;
            }
            else
            {
                simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.AfterPitWindow;
            }
        }

        private void AddTyresAndFuelInfo(SimulatorDataSet simData, AccShared accData)
        {
            if (_brakePadsStopwatch.Elapsed.Minutes > 5)
            {
                Logger.Info($"Brake Pads:  {accData.AcsPhysics.padLife[(int)AcWheels.FL]:N1}, Disc Life : {accData.AcsPhysics.discLifeLife[(int)AcWheels.FL]:N1}");
                _brakePadsStopwatch.Restart();
            }

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear = 0;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear = 0;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear = 0;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear = 0;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Camber = Angle.GetFromRadians(accData.AcsPhysics.camberRAD[(int)AcWheels.FL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Camber = Angle.GetFromRadians(-accData.AcsPhysics.camberRAD[(int)AcWheels.FR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Camber = Angle.GetFromRadians(accData.AcsPhysics.camberRAD[(int)AcWheels.RL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Camber = Angle.GetFromRadians(-accData.AcsPhysics.camberRAD[(int)AcWheels.RR]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = accData.AcsPhysics.wheelAngularSpeed[(int)AcWheels.FL];
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = accData.AcsPhysics.wheelAngularSpeed[(int)AcWheels.FR];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = accData.AcsPhysics.wheelAngularSpeed[(int)AcWheels.RL];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = accData.AcsPhysics.wheelAngularSpeed[(int)AcWheels.RR];

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.DirtLevel = accData.AcsPhysics.tyreDirtyLevel[(int)AcWheels.FL];
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.DirtLevel = accData.AcsPhysics.tyreDirtyLevel[(int)AcWheels.FR];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.DirtLevel = accData.AcsPhysics.tyreDirtyLevel[(int)AcWheels.RL];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.DirtLevel = accData.AcsPhysics.tyreDirtyLevel[(int)AcWheels.RR];

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreLoad = Force.GetFromNewtons(accData.AcsPhysics.wheelLoad[(int)AcWheels.FL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreLoad = Force.GetFromNewtons(accData.AcsPhysics.wheelLoad[(int)AcWheels.FR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreLoad = Force.GetFromNewtons(accData.AcsPhysics.wheelLoad[(int)AcWheels.RL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreLoad = Force.GetFromNewtons(accData.AcsPhysics.wheelLoad[(int)AcWheels.RR]);

            simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled = false;
            simData.PlayerInfo.CarInfo.FrontHeight = Distance.FromMeters(accData.AcsPhysics.rideHeight[0]);
            simData.PlayerInfo.CarInfo.RearHeight = Distance.FromMeters(accData.AcsPhysics.rideHeight[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight = simData.PlayerInfo.CarInfo.FrontHeight;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight = simData.PlayerInfo.CarInfo.FrontHeight;

            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight = simData.PlayerInfo.CarInfo.RearHeight;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight = simData.PlayerInfo.CarInfo.RearHeight;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromMeters(accData.AcsPhysics.suspensionTravel[(int)AcWheels.FL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromMeters(accData.AcsPhysics.suspensionTravel[(int)AcWheels.FR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromMeters(accData.AcsPhysics.suspensionTravel[(int)AcWheels.RL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromMeters(accData.AcsPhysics.suspensionTravel[(int)AcWheels.RR]);

            Temperature idealTyreTemp;
            Temperature idealTyreTempWindow;

            /*
             * According to the data found here (https://github.com/notsissyHex/ACCBoP) there shouldn't be a difference between ideal
             * pressures for GT4 and GT3 anymore. There also isn't a lot of info regarding pressure windows, so I'm opting to omit
             * a window instead. Comparing compounds also allows setting the correct tyre temperature values.
             */
            if (accData.AcsGraphic.tyreCompound == "dry_compound")
            {
                const double dryMaxTemp = 90.0;
                const double dryMinTemp = 76.0;
                idealTyreTemp = Temperature.FromCelsius((dryMinTemp + dryMaxTemp) / 2);
                idealTyreTempWindow = Temperature.FromCelsius((dryMaxTemp - dryMinTemp) / 2);

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.FL]);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.IdealQuantity = Pressure.FromPsi(26.60);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.3); //Needs a window for pressure coloring to work

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.FR]);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.IdealQuantity = Pressure.FromPsi(26.60);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.3); //Needs a window for pressure coloring to work

                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.RL]);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.IdealQuantity = Pressure.FromPsi(26.60);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.3); //Needs a window for pressure coloring to work

                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.RR]);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.IdealQuantity = Pressure.FromPsi(26.60);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.3); //Needs a window for pressure coloring to work

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = "DRY";
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreSet = accData.AcsGraphic.currentTyreSet;

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreType = "DRY";
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreSet = accData.AcsGraphic.currentTyreSet;

                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreType = "DRY";
                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreSet = accData.AcsGraphic.currentTyreSet;

                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreType = "DRY";
                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreSet = accData.AcsGraphic.currentTyreSet;
            }
            else
            {
                const double wetMaxTemp = 65.0;
                const double wetMinTemp = 25.4;
                idealTyreTemp = Temperature.FromCelsius((wetMinTemp + wetMaxTemp) / 2);
                idealTyreTempWindow = Temperature.FromCelsius((wetMaxTemp - wetMinTemp) / 2);

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.FL]);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.IdealQuantity = Pressure.FromPsi(30);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.5);

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.FR]);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.IdealQuantity = Pressure.FromPsi(30);
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.5);

                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.RL]);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.IdealQuantity = Pressure.FromPsi(30);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.5);

                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity = Pressure.FromPsi(accData.AcsPhysics.wheelsPressure[(int)AcWheels.RR]);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.IdealQuantity = Pressure.FromPsi(30);
                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.IdealQuantityWindow = Pressure.FromPsi(0.5);

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = "WET";
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreSet = accData.AcsGraphic.currentTyreSet;

                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreType = "WET";
                simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreSet = accData.AcsGraphic.currentTyreSet;

                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreType = "WET";
                simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreSet = accData.AcsGraphic.currentTyreSet;

                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreType = "WET";
                simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreSet = accData.AcsGraphic.currentTyreSet;
            }

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.IdealQuantity = idealTyreTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.IdealQuantityWindow = idealTyreTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.tyreCoreTemperature[(int)AcWheels.FL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature;

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.IdealQuantity = idealTyreTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.IdealQuantityWindow = idealTyreTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.tyreCoreTemperature[(int)AcWheels.FR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature;

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.IdealQuantity = idealTyreTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.IdealQuantityWindow = idealTyreTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.tyreCoreTemperature[(int)AcWheels.RL]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature;

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.IdealQuantity = idealTyreTemp;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.IdealQuantityWindow = idealTyreTempWindow;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(accData.AcsPhysics.tyreCoreTemperature[(int)AcWheels.RR]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp = simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature;

            // Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(accData.AcsStatic.maxFuel);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(accData.AcsPhysics.fuel);
        }

        internal void AddDriversData(SimulatorDataSet data, AccShared accData, FullUdpData udpData)
        {
            List<DriverInfo> driverInfos = new List<DriverInfo>();
            DriverInfo playersInfo = null;

            //60 is the size of accData.AcsGraphic.carIDs[] array. If more than 59 cars are present, then this cannot be used.
            bool numberOfCarsExceedSharedMemory = accData.AcsGraphic.carCount >= 59;

            IEnumerable<(FullCarData FullCarData, int CarSharedMemoryIndex)> allCarData = accData.AcsGraphic.status == AcStatus.AcOffSpectate || numberOfCarsExceedSharedMemory
                ? GetCarListFromBroadCast(data, accData, udpData)
                : GetCarListFromSharedMemory(data, accData, udpData);

            if (numberOfCarsExceedSharedMemory)
            {
                data.SimulatorSourceInfo.WorldPositionInvalid = true;
            }

            //foreach (var carData in udpData.CarsDictionary.Values.Where(x => accData.AcsGraphic.carCount > 0 || x.TimeSinceLastUpdate < StaleCarDataTime))
            foreach ((FullCarData udpCarData, int carSharedMemoryIndex) in allCarData)
            {
                if (udpCarData.LastUpdate.CurrentLap == null)
                {
                    continue;
                }

                DriverInfo driverInfo = CreateDriverInfo(accData, udpCarData, udpData.CurrentTrack, carSharedMemoryIndex, data, udpData);
                if (string.IsNullOrEmpty(driverInfo.DriverSessionId))
                {
                    continue;
                }

                if (data.SessionInfo.SessionType == SessionType.Race)
                {
                    driverInfo.CurrentLapValid = !udpCarData.LastUpdate.CurrentLap.IsInvalid;
                }
                else
                {
                    driverInfo.CurrentLapValid = udpCarData.LastUpdate.CurrentLap.Type == LapType.Regular && udpCarData.LastUpdate.CurrentLap.IsValidForBest && !udpCarData.LastUpdate.CurrentLap.IsInvalid;
                }

                if (driverInfo.IsPlayer)
                {
                    driverInfo.StintTimeLeft = TimeSpan.FromMilliseconds(Math.Min(accData.AcsGraphic.driverStintTimeLeft, accData.AcsGraphic.driverStintTotalTimeLeft));
                    playersInfo = driverInfo;
                }

                driverInfos.Add(driverInfo);
                if (driverInfo.Position == 1)
                {
                    data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                    data.LeaderInfo = driverInfo;
                }

                AddLappingInformation(data, udpData.CurrentTrack, driverInfo);
                FillTimingInfo(driverInfo, accData, udpCarData, data);

                if (data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector1) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector2) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector3))
                {
                    driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
                }
            }

            _lastPlayer = playersInfo;
            if (playersInfo != null)
            {
                data.PlayerInfo = playersInfo;
            }

            data.DriversInfo = driverInfos.ToArray();
        }

        private IEnumerable<(FullCarData FullCarData, int CarSharedMemoryIndex)> GetCarListFromSharedMemory(SimulatorDataSet data, AccShared accData, FullUdpData udpData)
        {
            for (int i = 0; i < accData.AcsGraphic.carCount; i++)
            {
                int carId = accData.AcsGraphic.carIDs[i];
                if (udpData.CarsDictionary.TryGetValue(carId, out FullCarData carData))
                {
                    yield return (carData, i);
                }
            }
        }

        private IEnumerable<(FullCarData FullCarData, int CarSharedMemoryIndex)> GetCarListFromBroadCast(SimulatorDataSet data, AccShared accData, FullUdpData udpData)
        {
            return udpData.CarsDictionary.Values.Where(x => x.TimeSinceLastUpdate < StaleCarDataTime).Select(x => (x, 0));
        }

        private void CheckPitDuration(SimulatorDataSet data, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType != SessionType.Race)
            {
                return;
            }

            bool hasEntry = _pitDurationMap.TryGetValue(driverInfo.DriverSessionId, out Stopwatch stopwatch);
            if (!driverInfo.InPits && hasEntry)
            {
                _pitDurationMap.Remove(driverInfo.DriverSessionId);
                return;
            }

            if (driverInfo.InPits && !hasEntry)
            {
                _pitDurationMap.Add(driverInfo.DriverSessionId, Stopwatch.StartNew());
                return;
            }

            if (driverInfo.InPits && hasEntry && stopwatch.Elapsed.TotalSeconds > 120 && driverInfo.Speed.InKph < 10)
            {
                driverInfo.FinishStatus = DriverFinishStatus.Dnf;
            }
        }

        private void FillTimingInfo(DriverInfo driverInfo, AccShared accShared, FullCarData carData, SimulatorDataSet dataSet)
        {
            driverInfo.Timing.CurrentSector = -1;
            driverInfo.Timing.LastLapTime = CreateTimeSpan(carData.LastUpdate.LastLap.LaptimeMS ?? 0);
            driverInfo.Timing.CurrentLapTime = CreateTimeSpan(carData.LastUpdate.CurrentLap.LaptimeMS ?? 0);

            if (dataSet.SessionInfo.SessionType == SessionType.Race && driverInfo.CompletedLaps == 0)
            {
                return;
            }

            TimeSpan[] splits = GetCurrentSplitTimes(driverInfo.DriverSessionId);
            int currentSplitIndex = -1;

            if (_sectorDistances == null)
            {
                _sectorDistances = _settingsRepository.LoadOrCreateDefault(dataSet.SessionInfo.TrackInfo.TrackFullName);
            }

            if (driverInfo.IsPlayer && _isAccSharedMemoryActive)
            {
                driverInfo.Timing.LastLapTime = CreateTimeSpan(accShared.AcsGraphic.iLastTime);
                driverInfo.Timing.CurrentLapTime = CreateTimeSpan(accShared.AcsGraphic.iCurrentTime);
                currentSplitIndex = accShared.AcsGraphic.currentSectorIndex;

                if (_playerLastSector == 0 && currentSplitIndex == 1 && !driverInfo.InPits && _sectorDistances.TryAddSector1Sample(driverInfo.LapDistance))
                {
                    _settingsRepository.Save(dataSet.SessionInfo.TrackInfo.TrackFullName, _sectorDistances);
                }
                else if (_playerLastSector == 1 && currentSplitIndex == 2 && !driverInfo.InPits && _sectorDistances.TryAddSector2Sample(driverInfo.LapDistance))
                {
                    _settingsRepository.Save(dataSet.SessionInfo.TrackInfo.TrackFullName, _sectorDistances);
                }

                _playerLastSector = currentSplitIndex;
            }
            else
            {
                driverInfo.Timing.LastLapTime = CreateTimeSpan(carData.LastUpdate.LastLap.LaptimeMS);
                driverInfo.Timing.CurrentLapTime = CreateTimeSpan(carData.LastUpdate.CurrentLap.LaptimeMS);

                if (driverInfo.LapDistance < _sectorDistances.SectorOneDistance || driverInfo.Timing.CurrentLapTime.TotalSeconds < 10)
                {
                    currentSplitIndex = 0;
                }
                else if (driverInfo.LapDistance < _sectorDistances.SectorTwoDistance)
                {
                    currentSplitIndex = 1;
                }
                else if (_sectorDistances.SectorOneDistance.HasValue && _sectorDistances.SectorTwoDistance.HasValue)
                {
                    currentSplitIndex = 2;
                }

                if (currentSplitIndex < 0)
                {
                    return;
                }
            }

            splits[currentSplitIndex] = driverInfo.Timing.CurrentLapTime;
            driverInfo.Timing.CurrentSector = currentSplitIndex + 1;

            driverInfo.Timing.LastSector1Time = splits[0];
            if (splits[0] != TimeSpan.Zero)
            {
                driverInfo.Timing.LastSector2Time = splits[1] - splits[0];
            }

            if (splits[1] != TimeSpan.Zero)
            {
                driverInfo.Timing.LastSector3Time = splits[2] - splits[1];
            }

            driverInfo.Timing.CurrentSectorTime = splits[currentSplitIndex];
        }

        private TimeSpan[] GetCurrentSplitTimes(string driverId)
        {
            if (!_currentSectorTimes.ContainsKey(driverId))
            {
                _currentSectorTimes[driverId] = new[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero };
            }

            return _currentSectorTimes[driverId];
        }

        private TimeSpan CreateTimeSpan(int? miliseconds)
        {
            return miliseconds > 0 ? TimeSpan.FromMilliseconds(miliseconds.Value) : TimeSpan.Zero;
        }

        private void AddLappingInformation(SimulatorDataSet data, TrackData trackData, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType == SessionType.Race && _lastPlayer != null
                                                                 && _lastPlayer.CompletedLaps != 0)
            {
                driverInfo.IsBeingLappedByPlayer =
                    driverInfo.TotalDistance < (_lastPlayer.TotalDistance - (trackData.TrackMeters * 0.5));
                driverInfo.IsLappingPlayer =
                    _lastPlayer.TotalDistance < (driverInfo.TotalDistance - (trackData.TrackMeters * 0.5));
            }
        }

        private DriverInfo CreateDriverInfo(AccShared accData, FullCarData carData, TrackData trackData, int carIndex, SimulatorDataSet dataSet, FullUdpData fullUdpData)
        {
            DriverInfo driverInfo = new DriverInfo
            {
                DriverSessionId = carData.LongName + carData.CarInfo.CarIndex,
                CompletedLaps = carData.LastUpdate.Laps,
                CarName = carData.CarName,
                InPits = carData.LastUpdate.CarLocation == CarLocationEnum.Pitlane || carData.LastUpdate.CarLocation == CarLocationEnum.NONE,
                IsPlayer = carData.CarInfo.CarIndex == accData.AcsGraphic.playerCarID && accData.AcsGraphic.status != AcStatus.AcOffSpectate,
                Position = carData.LastUpdate.Position,
                Speed = Velocity.FromKph(carData.LastUpdate.Kmh),
                LapDistance = trackData.TrackMeters * carData.LastUpdate.SplinePosition,
                FinishStatus = FromAcStatus(dataSet.SessionInfo.SessionPhase, fullUdpData, carData),
                WorldPosition = new Point3D(Distance.FromMeters(accData.AcsGraphic.carCoordinates[carIndex].x), Distance.FromMeters(accData.AcsGraphic.carCoordinates[carIndex].y), Distance.FromMeters(accData.AcsGraphic.carCoordinates[carIndex].z)),
                CarClassName = carData.ClassName,
                PositionInClass = carData.LastUpdate.Position,
                DriverShortName = carData.ShortName,
                DriverLongName = carData.LongName,
                TeamName = carData.CarInfo.TeamName,
                CarRaceNumber = carData.CarInfo.RaceNumber,
                IsCarRaceNumberFilled = true,
                CarClassId = "ACC-" + carData.ClassName,
            };

            if (driverInfo.IsPlayer && !string.IsNullOrWhiteSpace(_customPlayerName))
            {
                driverInfo.DriverLongName = _customPlayerName;
                driverInfo.DriverShortName = _customPlayerName;
            }

            driverInfo.TotalDistance = (driverInfo.CompletedLaps * trackData.TrackMeters) + driverInfo.LapDistance;
            CheckPitDuration(dataSet, driverInfo);

            ComputeDistanceToPlayer(_lastPlayer, driverInfo, trackData);
            return driverInfo;
        }

        internal static void ComputeDistanceToPlayer(DriverInfo player, DriverInfo driverInfo, TrackData trackData)
        {
            if (player == null)
            {
                return;
            }

            if (driverInfo.FinishStatus == DriverFinishStatus.Dq || driverInfo.FinishStatus == DriverFinishStatus.Dnf ||
                driverInfo.FinishStatus == DriverFinishStatus.Dnq || driverInfo.FinishStatus == DriverFinishStatus.Dns)
            {
                driverInfo.DistanceToPlayer = double.MaxValue;
                return;
            }

            double trackLength = trackData.TrackMeters;
            double playerLapDistance = player.LapDistance;

            double distanceToPlayer = playerLapDistance - driverInfo.LapDistance;
            if (distanceToPlayer < -(trackLength / 2))
            {
                distanceToPlayer += trackLength;
            }

            if (distanceToPlayer > (trackLength / 2))
            {
                distanceToPlayer -= trackLength;
            }

            driverInfo.DistanceToPlayer = distanceToPlayer;
        }

        internal static DriverFinishStatus FromAcStatus(SessionPhase sessionPhase, FullUdpData fullUdpData, FullCarData carData)
        {
            if (carData.IsFinished && (fullUdpData.LastRealtimeUpdate.Phase == UDPData.SessionPhase.SessionOver || sessionPhase == SessionPhase.Checkered))
            {
                return DriverFinishStatus.Finished;
            }

            if (carData.LastUpdate.CarLocation == CarLocationEnum.NONE)
            {
                return DriverFinishStatus.Dnf;
            }

            return DriverFinishStatus.None;
        }
    }
}
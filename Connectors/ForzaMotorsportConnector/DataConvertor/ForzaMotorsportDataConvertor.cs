namespace SecondMonitor.ForzaMotorsportConnector.DataConvertor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using Foundation.Connectors;

    using Newtonsoft.Json;

    using SecondMonitor.DataModel.BasicProperties.Units;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.ForzaMotorsportConnector.DataModels;
    using SecondMonitor.ForzaMotorsportConnector.TyreProperties;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;

    public class ForzaMotorsportDataConvertor : AbstractDataConvertor
    {
        private readonly ForzaMotorsportTyreProperties _forzaMotorsportTyreProperties;
        public const string ConnectorName = SimulatorsNameMap.ForzaMotorsportSimName;
        private readonly Dictionary<string, ForzaMotorsportTrackInfo> _tracksInfo;
        private readonly Dictionary<string, ForzaMotorsportCarInfo> _carsInfo;
        private readonly Dictionary<string, ForzaMotorsportCarTrackWidthInfo> _carsTrackWidthInfo;
        private readonly string _driverName;
        private int _currentLap = -1;
        private double _distanceTraveledForCompletedLapsInMeters = 0.0;
        private TimeSpan[] _currentSectorTimes = [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero];

        public ForzaMotorsportDataConvertor(ForzaMotorsportConfiguration configuration)
        {
            _forzaMotorsportTyreProperties = new ForzaMotorsportTyreProperties();
            _driverName = configuration.DriverName;
            _tracksInfo = LoadTracksInfo();
            _carsInfo = LoadCarsInfo();
            _carsTrackWidthInfo = LoadCarsTrackWidthInfo();
        }

        internal void Reset()
        {
            _currentLap = -1;
            _distanceTraveledForCompletedLapsInMeters = -1.0;
            _currentSectorTimes = [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero];
        }

        private Dictionary<string, ForzaMotorsportTrackInfo> LoadTracksInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SecondMonitor.ForzaMotorsportConnector.DataModels.ForzaMotorsportTracksKeys.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, ForzaMotorsportTrackInfo>>(json);
            }
        }

        private Dictionary<string, ForzaMotorsportCarInfo> LoadCarsInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SecondMonitor.ForzaMotorsportConnector.DataModels.ForzaMotorsportCarsKeys.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, ForzaMotorsportCarInfo>>(json);
            }
        }

        private Dictionary<string, ForzaMotorsportCarTrackWidthInfo> LoadCarsTrackWidthInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SecondMonitor.ForzaMotorsportConnector.DataModels.ForzaMotorsportCarsTrackWidthKeys.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, ForzaMotorsportCarTrackWidthInfo>>(json);
            }
        }

        public SimulatorDataSet CreateSimulatorDataSet(ForzaMotorsportUDPTelemetryData forzaMotorsportData)
        {
            SimulatorDataSet simData = new SimulatorDataSet(ConnectorName)
            {
                SimulatorSourceInfo =
                {
                    HasLapTimeInformation = true,
                    OutLapIsValid = true,
                    InvalidateLapBySector = true,
                    HasRewindFunctionality = true,
                    TimeInterpolated = true,
                    OverrideBestLap = true,
                    GapInformationProvided = GapInformationKind.None,
                    SectorTimingSupport = DataInputSupport.PlayerOnly,
                    DetermineLapsUntilHeavyWearByLightWear = true,
                    TelemetryInfo = {
                        ContainsSuspensionTravel = true,
                        ContainsSuspensionVelocity = false,
                        ContainsSlipInformation = true,
                        ContainsWheelRps = true
                    }
                }
            };

            FillSessionInfo(forzaMotorsportData, simData, CreateTimeSpan((double)forzaMotorsportData.CurrentRaceTime));
            AddDriversData(simData, forzaMotorsportData);

            FillPlayerCarInfo(forzaMotorsportData, simData);

            //// PEDAL INFO
            AddPedalInfo(forzaMotorsportData, simData);

            //// Tyre Pressure Info
            AddTyresAndFuelInfo(forzaMotorsportData, simData);

            //// Add Roll Roll Angle Info
            AddRollAngleInfo(forzaMotorsportData, simData);

            //// Add Additional Player Car Info
            AddPlayerCarInfo(forzaMotorsportData, simData);

            //// Acceleration
            AddAcceleration(forzaMotorsportData, simData);

            _forzaMotorsportTyreProperties.FillWheelIdealQuantities(simData);

            return simData;
        }

        private static TimeSpan CreateTimeSpan(double seconds)
        {
            return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        }

        private static void AddPedalInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = (double)data.Accel / byte.MaxValue;
            simData.InputInfo.BrakePedalPosition = (double)data.Brake / byte.MaxValue;
            simData.InputInfo.ClutchPedalPosition = (double)data.Clutch / byte.MaxValue;
            simData.InputInfo.SteeringInput = Math.Clamp((double)data.Steer / sbyte.MaxValue, -1.0, 1.0);
        }

        private static void FillPlayerCarInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.EngineRpm = (int)data.CurrentEngineRpm;
            switch (data.Gear)
            {
                case 0:
                    simData.PlayerInfo.CarInfo.CurrentGear = "R";
                    break;
                case 11:
                    simData.PlayerInfo.CarInfo.CurrentGear = "N";
                    break;
                default:
                    simData.PlayerInfo.CarInfo.CurrentGear = data.Gear.ToString();
                    break;
            }
        }

        private void AddRollAngleInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            if (_carsTrackWidthInfo.TryGetValue(data.CarOrdinal.ToString(), out var carsTrackWidthInfo))
            {
                double frontRollAngle = (simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel.InMeters - simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel.InMeters) / carsTrackWidthInfo.FrontTrackWidth;
                double rearRollAngle = (simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel.InMeters - simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel.InMeters) / carsTrackWidthInfo.RearTrackWidth;
                simData.PlayerInfo.CarInfo.FrontRollAngle = Angle.GetFromRadians(frontRollAngle);
                simData.PlayerInfo.CarInfo.RearRollAngle = Angle.GetFromRadians(rearRollAngle);
            }
        }

        private void AddPlayerCarInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;
            playerCar.EngineRpm = (int)data.CurrentEngineRpm;
            playerCar.EngineTorque = Torque.FromNm(data.Torque);
            playerCar.EnginePower = Power.FromKw(data.Torque * data.CurrentEngineRpm / 9548.8);
            playerCar.TurboPressure = Pressure.FromPsi(data.Boost);
            playerCar.WorldOrientation = new Orientation()
            {
                Pitch = Angle.GetFromRadians(data.Pitch),
                Yaw = Angle.GetFromRadians(data.Yaw),
                Roll = Angle.GetFromRadians(data.Roll),
            };
        }

        private static void AddAcceleration(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinMs = data.AccelerationX;
            simData.PlayerInfo.CarInfo.Acceleration.YinMs = data.AccelerationY;
            simData.PlayerInfo.CarInfo.Acceleration.ZinMs = -data.AccelerationZ;
        }

        private void AddTyresAndFuelInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            const double NoWearWearLimit = 0.47; // green
            const double LightWearLimit = 0.5; // yellow
            const double HeavyWearLimit = 0.8; // red

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.NoWearWearLimit = NoWearWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.NoWearWearLimit = NoWearWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.NoWearWearLimit = NoWearWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.NoWearWearLimit = NoWearWearLimit;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.LightWearLimit = LightWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.LightWearLimit = LightWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.LightWearLimit = LightWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.LightWearLimit = LightWearLimit;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.HeavyWearLimit = HeavyWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.HeavyWearLimit = HeavyWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.HeavyWearLimit = HeavyWearLimit;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.HeavyWearLimit = HeavyWearLimit;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear = data.TireWearFrontLeft;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear = data.TireWearFrontRight;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear = data.TireWearRearLeft;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear = data.TireWearRearRight;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = data.WheelRotationSpeedFrontLeft;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = data.WheelRotationSpeedFrontRight;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = data.WheelRotationSpeedRearLeft;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = data.WheelRotationSpeedRearRight;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Slip = Math.Min(data.TireSlipRatioFrontLeft, 1.0);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Slip = Math.Min(data.TireSlipRatioFrontRight, 1.0);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Slip = Math.Min(data.TireSlipRatioRearLeft, 1.0);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Slip = Math.Min(data.TireSlipRatioRearRight, 1.0);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromMeters(data.SuspensionTravelMetersFrontLeft);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromMeters(data.SuspensionTravelMetersFrontRight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromMeters(data.SuspensionTravelMetersRearLeft);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromMeters(data.SuspensionTravelMetersRearRight);

            //// Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromFahrenheit(data.TireTempFrontLeft);

            //// Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity = Temperature.FromFahrenheit(data.TireTempFrontRight);

            //// Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromFahrenheit(data.TireTempRearLeft);

            //// Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity = Temperature.FromFahrenheit(data.TireTempRearRight);

            //// Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(100);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(data.Fuel * 100);
        }

        internal void AddDriversData(SimulatorDataSet data, ForzaMotorsportUDPTelemetryData forzaMotorsportData)
        {
            data.DriversInfo = new DriverInfo[1];
            DriverInfo driverInfo = CreateDriverInfo(forzaMotorsportData, data);
            driverInfo.CurrentLapValid = true;
            data.DriversInfo[0] = driverInfo;
            
            if (driverInfo.Position == 1)
            {
                data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                data.LeaderInfo = driverInfo;
            }

            FillTimingInfo(driverInfo, forzaMotorsportData);

            data.PlayerInfo = driverInfo;
        }

        internal void FillTimingInfo(DriverInfo driverInfo, ForzaMotorsportUDPTelemetryData data)
        {
            driverInfo.Timing.LastLapTime = CreateTimeSpan(data.LastLap);
            driverInfo.Timing.CurrentLapTime = CreateTimeSpan(data.CurrentLap);
            driverInfo.Timing.BestLapTime = CreateTimeSpan(data.BestLap);

            if (_distanceTraveledForCompletedLapsInMeters < 0.0)
            {
                driverInfo.Timing.CurrentSector = -1;
                return;
            }

            Distance sectorLength = Distance.FromMeters(_distanceTraveledForCompletedLapsInMeters / (double)_currentLap / 3.0);

            if (driverInfo.LapDistance >= 0 && driverInfo.LapDistance <= sectorLength.InMeters)
            {
                driverInfo.Timing.CurrentSector = 1;
                driverInfo.Timing.CurrentSectorTime = driverInfo.Timing.CurrentLapTime;
                _currentSectorTimes[0] = driverInfo.Timing.CurrentSectorTime;
            }
            else if (driverInfo.LapDistance > sectorLength.InMeters && driverInfo.LapDistance <= sectorLength.InMeters * 2.0)
            {
                driverInfo.Timing.CurrentSector = 2;
                driverInfo.Timing.CurrentSectorTime = driverInfo.Timing.CurrentLapTime - _currentSectorTimes[0];
                _currentSectorTimes[1] = driverInfo.Timing.CurrentSectorTime;
            }
            else if (driverInfo.LapDistance > sectorLength.InMeters * 2.0)
            {
                driverInfo.Timing.CurrentSector = 3;
                driverInfo.Timing.CurrentSectorTime = driverInfo.Timing.CurrentLapTime - _currentSectorTimes[0] - _currentSectorTimes[1];
                _currentSectorTimes[2] = driverInfo.Timing.CurrentSectorTime;
            }

            driverInfo.Timing.LastSector1Time = _currentSectorTimes[0];
            driverInfo.Timing.LastSector2Time = _currentSectorTimes[1];
            driverInfo.Timing.LastSector3Time = _currentSectorTimes[2];
        }

        private DriverInfo CreateDriverInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData)
        {
            DriverInfo driverInfo = new DriverInfo
            {
                DriverSessionId = _driverName,
                CompletedLaps = (int)data.LapNumber,
                Position = (int)data.RacePosition,
                PositionInClass = (int)data.RacePosition,
                IsCarRaceNumberFilled = false,
                IsPlayer = true,
                InPits = false
            };

            if (_currentLap != driverInfo.CompletedLaps)
            {
                if (_currentLap != -1)
                {
                    _distanceTraveledForCompletedLapsInMeters = data.DistanceTraveled;
                }

                _currentLap = driverInfo.CompletedLaps;
            }

            if (_carsInfo.TryGetValue(data.CarOrdinal.ToString(), out var carInfo))
            {
                driverInfo.CarName = $"{carInfo.Year} {carInfo.Make} {carInfo.Model}";
                driverInfo.CarClassName = $"{((ForzaMotorsportCarClass)data.CarClass).ToString()}{data.CarPerformanceIndex.ToString()}";
                driverInfo.CarClassId = ((ForzaMotorsportCarClass)data.CarClass).ToString();
            }

            driverInfo.DriverShortName = driverInfo.DriverLongName = driverInfo.DriverSessionId;
            driverInfo.Speed = Velocity.FromMs(data.Speed);
            driverInfo.LapDistance = (double)data.DistanceTraveled - _distanceTraveledForCompletedLapsInMeters;
            driverInfo.FinishStatus = DriverFinishStatus.Na;
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(-data.PositionX), Distance.FromMeters(data.PositionY), Distance.FromMeters(data.PositionZ));
            driverInfo.TotalDistance = data.DistanceTraveled;
            ComputeDistanceToPlayer(driverInfo, driverInfo, simData.SessionInfo.TrackInfo.LayoutLength.InMeters);
            
            return driverInfo;
        }

        internal void FillSessionInfo(ForzaMotorsportUDPTelemetryData data, SimulatorDataSet simData, TimeSpan sessionTime)
        {
            // Timing
            simData.SessionInfo.SessionTime = sessionTime;
            simData.SessionInfo.IsMultiClass = true;
            if (_tracksInfo.TryGetValue(data.TrackOrdinal.ToString(), out var trackInfo))
            {
                simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(trackInfo.LengthInKm * 1000);
                simData.SessionInfo.TrackInfo.TrackName = trackInfo.Circuit;
                simData.SessionInfo.TrackInfo.TrackLayoutName = trackInfo.Track;
            }

            simData.SessionInfo.WeatherInfo.HasWindInformation = false;
            simData.SessionInfo.SessionType = SessionType.Practice;
            simData.SessionInfo.SessionPhase = SessionPhase.Green;
            simData.SessionInfo.IsActive = data.IsRaceOn == 1;
            simData.SessionInfo.SessionLengthType = SessionLengthType.Na;
        }
    }
}
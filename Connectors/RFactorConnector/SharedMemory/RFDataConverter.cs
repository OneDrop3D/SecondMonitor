namespace SecondMonitor.RFactorConnector.SharedMemory
{
    using System;
    using System.Linq;

    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using Foundation.Connectors;

    using SecondMonitor.DataModel.Extensions;

    internal class RFDataConverter : AbstractDataConvertor
    {
        private const int MaxConsecutivePackagesIgnored = 200;
        private readonly AmsClassMapping _amsClassMapping;

        private DriverInfo _lastPlayer = new DriverInfo();
        private int _currentlyIgnoredPackage;
        private double _maxFuelCapacity;

        public RFDataConverter()
        {
            _amsClassMapping = new AmsClassMapping();
        }

        public string SourceName { get; set; }

        public SimulatorDataSet CreateSimulatorDataSet(RfShared rfData)
        {
            SimulatorDataSet simData = new SimulatorDataSet(SourceName)
            {
                SimulatorSourceInfo =
                {
                    GapInformationProvided = GapInformationKind.None,
                    HasLapTimeInformation = true,
                    SimNotReportingEndOfOutLapCorrectly = true,
                    OutLapIsValid = true,
                    InvalidateLapBySector = true,
                    SectorTimingSupport = DataInputSupport.Full,
                    TelemetryInfo = { ContainsSuspensionTravel = true, ContainsWheelRps = true },
                    NAStateBetweenSessions = true
                }
            };

            FillSessionInfo(rfData, simData);
            AddFlags(rfData, simData);
            AddDriversData(simData, rfData);

            FillPlayerCarInfo(rfData, simData);

            // PEDAL INFO
            AddPedalInfo(rfData, simData);

            // WaterSystemInfo
            AddWaterSystemInfo(rfData, simData);

            // OilSystemInfo
            AddOilSystemInfo(rfData, simData);

            // Brakes Info
            AddBrakesInfo(rfData, simData);

            // Tyre Pressure Info
            AddTyresAndFuelInfo(rfData, simData);

            // Acceleration
            AddAcceleration(rfData, simData);

            //Add Additional Player Car Info
            AddPlayerCarInfo(rfData, simData);

            _currentlyIgnoredPackage = 0;

            PopulateClassPositions(simData);
            return simData;
        }

        private void AddPlayerCarInfo(RfShared data, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;

            int totalDent = data.DentSeverity.Aggregate((x, y) => (byte)(x + y));
            int maxDent = data.DentSeverity.Max();
            playerCar.CarDamageInformation.Bodywork.Damage = totalDent / 16.0;
            if (maxDent == 1)
            {
                playerCar.CarDamageInformation.Bodywork.MediumDamageThreshold = playerCar.CarDamageInformation.Bodywork.Damage;
            }
            else if (maxDent == 2)
            {
                playerCar.CarDamageInformation.Bodywork.MediumDamageThreshold = 0;
                playerCar.CarDamageInformation.Bodywork.HeavyDamageThreshold = playerCar.CarDamageInformation.Bodywork.Damage;
            }

            if (data.Overheating == 1)
            {
                playerCar.CarDamageInformation.Engine.MediumDamageThreshold = 0.5;
                playerCar.CarDamageInformation.Engine.HeavyDamageThreshold = 1.1;
                playerCar.CarDamageInformation.Engine.Damage = 1;
            }

            playerCar.WorldOrientation = new Orientation
            {
                Yaw = Angle.GetFromRadians(Math.Atan2(data.OriZ.X, data.OriZ.Z))
            };

            playerCar.SpeedLimiterEngaged = false;
        }

        public void ResetOnSessionStart()
        {
            _maxFuelCapacity = 0;
        }

        private static void AddAcceleration(RfShared data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinMs = data.LocalAccel.X;
            simData.PlayerInfo.CarInfo.Acceleration.YinMs = data.LocalAccel.Y;
            simData.PlayerInfo.CarInfo.Acceleration.ZinMs = -data.LocalAccel.Z;
        }

        private static void AddBrakesInfo(RfShared data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontLeft].BrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontRight].BrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearLeft].BrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearRight].BrakeTemp);
        }

        private static void AddOilSystemInfo(RfShared data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(data.EngineOilTemp);
        }

        private static void AddWaterSystemInfo(RfShared data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(data.EngineWaterTemp);
        }

        private static void AddPedalInfo(RfShared data, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = data.UnfilteredThrottle;
            simData.InputInfo.BrakePedalPosition = data.UnfilteredBrake;
            simData.InputInfo.ClutchPedalPosition = data.UnfilteredClutch;
            simData.InputInfo.SteeringInput = data.UnfilteredSteering;
        }

        internal static DriverFinishStatus FromRFStatus(int finishStatus)
        {
            switch ((RfFinishStatus)finishStatus)
            {
                case RfFinishStatus.None:
                    return DriverFinishStatus.None;
                case RfFinishStatus.Dnf:
                    return DriverFinishStatus.Dnf;
                case RfFinishStatus.Dq:
                    return DriverFinishStatus.Dq;
                case RfFinishStatus.Finished:
                    return DriverFinishStatus.Finished;
                default:
                    return DriverFinishStatus.Na;
            }
        }

        private static void FillPlayerCarInfo(RfShared data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.EngineRpm = (int)data.EngineRPM;
            switch (data.Gear)
            {
                case 0:
                    simData.PlayerInfo.CarInfo.CurrentGear = "N";
                    break;
                case -1:
                    simData.PlayerInfo.CarInfo.CurrentGear = "R";
                    break;
                case -2:
                    simData.PlayerInfo.CarInfo.CurrentGear = string.Empty;
                    break;
                default:
                    simData.PlayerInfo.CarInfo.CurrentGear = data.Gear.ToString();
                    break;
            }
        }

        private void AddFlags(RfShared rfData, SimulatorDataSet simData)
        {
            if ((RfGamePhase)rfData.GamePhase == RfGamePhase.FullCourseYellow)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.FullCourseYellow;
                return;
            }

            if (rfData.SectorFlag[1] == 2)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
            }

            if (rfData.SectorFlag[2] == 2)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
            }

            if (rfData.SectorFlag[0] == 2)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
            }
        }

        private void AddTyresAndFuelInfo(RfShared data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.Wheel[(int)RfWheelIndex.FrontLeft].Pressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.Wheel[(int)RfWheelIndex.FrontRight].Pressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.Wheel[(int)RfWheelIndex.RearLeft].Pressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.Wheel[(int)RfWheelIndex.RearRight].Pressure);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Detached = data.Wheel[(int)RfWheelIndex.FrontLeft].Detached == 1 || data.Wheel[(int)RfWheelIndex.FrontLeft].Flat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Detached = data.Wheel[(int)RfWheelIndex.FrontRight].Detached == 1 || data.Wheel[(int)RfWheelIndex.FrontRight].Flat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Detached = data.Wheel[(int)RfWheelIndex.RearLeft].Detached == 1 || data.Wheel[(int)RfWheelIndex.RearLeft].Flat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Detached = data.Wheel[(int)RfWheelIndex.RearRight].Detached == 1 || data.Wheel[(int)RfWheelIndex.RearRight].Flat == 1;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear = 1 - data.Wheel[(int)RfWheelIndex.FrontLeft].Wear;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear = 1 - data.Wheel[(int)RfWheelIndex.FrontRight].Wear;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear = 1 - data.Wheel[(int)RfWheelIndex.RearLeft].Wear;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear = 1 - data.Wheel[(int)RfWheelIndex.RearRight].Wear;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = "Prime";
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreType = "Prime";
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreType = "Prime";
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreType = "Prime";

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = -data.Wheel[(int)RfWheelIndex.FrontLeft].Rotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = -data.Wheel[(int)RfWheelIndex.FrontRight].Rotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = -data.Wheel[(int)RfWheelIndex.RearLeft].Rotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = -data.Wheel[(int)RfWheelIndex.RearRight].Rotation;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.FrontLeft].SuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.FrontRight].SuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.RearLeft].SuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.RearRight].SuspensionDeflection);

            simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled = true;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.FrontLeft].RideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.FrontRight].RideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.RearLeft].RideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight = Distance.FromMeters(data.Wheel[(int)RfWheelIndex.RearRight].RideHeight);

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontLeft].Temperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontLeft].Temperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontLeft].Temperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontLeft].Temperature[1]);

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontRight].Temperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontRight].Temperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontRight].Temperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.FrontRight].Temperature[1]);

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearLeft].Temperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearLeft].Temperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearLeft].Temperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearLeft].Temperature[1]);

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearRight].Temperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearRight].Temperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearRight].Temperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity = Temperature.FromKelvin(data.Wheel[(int)RfWheelIndex.RearRight].Temperature[1]);

            // Fuel System
            _maxFuelCapacity = Math.Max(_maxFuelCapacity, data.Fuel);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(Math.Max(50, _maxFuelCapacity));
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(data.Fuel);
        }

        internal void AddDriversData(SimulatorDataSet data, RfShared rfData)
        {
            if (rfData.NumVehicles < 1)
            {
                return;
            }

            data.DriversInfo = new DriverInfo[rfData.NumVehicles];
            DriverInfo playersInfo = null;

            for (int i = 0; i < rfData.NumVehicles; i++)
            {
                RfVehicleInfo rfVehicleInfo = rfData.Vehicle[i];
                DriverInfo driverInfo = CreateDriverInfo(rfData, rfVehicleInfo);

                if (driverInfo.IsPlayer)
                {
                    playersInfo = driverInfo;
                    driverInfo.CurrentLapValid = true;
                }
                else
                {
                    driverInfo.CurrentLapValid = true;
                }

                data.DriversInfo[i] = driverInfo;
                if (driverInfo.Position == 1)
                {
                    data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                    data.LeaderInfo = driverInfo;
                }

                if (rfVehicleInfo.Control == 2)
                {
                    data.SessionInfo.IsMultiplayer = true;
                }

                AddLappingInformation(data, rfData, driverInfo);
                FillTimingInfo(driverInfo, rfVehicleInfo, rfData);

                if (rfData.SectorFlag[1] == 2 || rfData.SectorFlag[2] == 2 || rfData.SectorFlag[0] == 2)
                {
                    driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
                }
            }

            CheckValidityByPlayer(playersInfo);
            _lastPlayer = playersInfo;
            if (playersInfo != null)
            {
                data.PlayerInfo = playersInfo;
            }

            //FillGapInformation(data.DriversInfo);
        }

        private void CheckValidityByPlayer(DriverInfo driver)
        {
            if (_lastPlayer?.WorldPosition == null || driver?.WorldPosition == null || (!_lastPlayer.InPits && driver.InPits))
            {
                return;
            }

            Distance distance = Point3D.GetDistance(driver.WorldPosition, _lastPlayer.WorldPosition);

            if (distance.InMeters > 200)
            {
                _currentlyIgnoredPackage++;
                if (_currentlyIgnoredPackage < MaxConsecutivePackagesIgnored)
                {
                    throw new RFInvalidPackageException("Players distance was :" + distance.InMeters);
                }
            }
        }

        internal void ValidateLapBasedOnSurface(DriverInfo driverInfo, RfVehicleInfo rfVehicleInfo)
        {
            if (!driverInfo.CurrentLapValid)
            {
            }
        }

        internal void FillTimingInfo(DriverInfo driverInfo, RfVehicleInfo rfVehicleInfo, RfShared rfShared)
        {
            driverInfo.Timing.GapAhead = TimeSpan.FromSeconds(rfVehicleInfo.TimeBehindNext);
            driverInfo.Timing.LastSector1Time = CreateTimeSpan(rfVehicleInfo.CurSector1);
            driverInfo.Timing.LastSector2Time = CreateTimeSpan(rfVehicleInfo.CurSector2 - rfVehicleInfo.CurSector1);
            driverInfo.Timing.LastSector3Time = CreateTimeSpan(rfVehicleInfo.LastLapTime - rfVehicleInfo.LastSector2);
            driverInfo.Timing.LastLapTime = CreateTimeSpan(rfVehicleInfo.LastLapTime);
            driverInfo.Timing.CurrentSector = rfVehicleInfo.Sector == 0 ? 3 : rfVehicleInfo.Sector;
            driverInfo.Timing.CurrentLapTime = CreateTimeSpan(rfShared.CurrentET - rfShared.LapStartET);
        }

        private TimeSpan CreateTimeSpan(double seconds)
        {
            return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        }

        private void AddLappingInformation(SimulatorDataSet data, RfShared rfData, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType == SessionType.Race && _lastPlayer != null
                && _lastPlayer.CompletedLaps != 0)
            {
                driverInfo.IsBeingLappedByPlayer =
                    driverInfo.TotalDistance < (_lastPlayer.TotalDistance - (rfData.LapDist * 0.5));
                driverInfo.IsLappingPlayer =
                    _lastPlayer.TotalDistance < (driverInfo.TotalDistance - (rfData.LapDist * 0.5));
            }
        }

        private DriverInfo CreateDriverInfo(RfShared rfData, RfVehicleInfo rfVehicleInfo)
        {
            DriverInfo driverInfo = new DriverInfo
                                        {
                                            DriverSessionId = rfVehicleInfo.DriverName.FromArray(),
                                            CompletedLaps = rfVehicleInfo.TotalLaps,
                                            CarName = rfVehicleInfo.VehicleName.FromArray(),
                                            InPits = rfVehicleInfo.InPits == 1
                                        };

            driverInfo.CarClassName = SourceName == SimulatorsNameMap.AmsSimName ? _amsClassMapping.MapClass(rfVehicleInfo.VehicleClass.FromArray()) : rfVehicleInfo.VehicleClass.FromArray();

            driverInfo.DriverShortName = driverInfo.DriverLongName = driverInfo.DriverSessionId;
            driverInfo.CarClassId = driverInfo.CarClassName;
            driverInfo.IsPlayer = rfVehicleInfo.IsPlayer == 1;
            driverInfo.Position = rfVehicleInfo.Place;
            driverInfo.PositionInClass = rfVehicleInfo.Place;
            driverInfo.Speed = Velocity.FromMs(rfVehicleInfo.Speed);
            driverInfo.LapDistance = rfVehicleInfo.LapDist >= 0 ? rfVehicleInfo.LapDist : rfData.LapDist + rfVehicleInfo.LapDist;
            driverInfo.TotalDistance = (rfVehicleInfo.TotalLaps * rfData.LapDist) + driverInfo.LapDistance;
            driverInfo.FinishStatus = FromRFStatus(rfVehicleInfo.FinishStatus);
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(-rfVehicleInfo.Pos.X), Distance.FromMeters(rfVehicleInfo.Pos.Y), Distance.FromMeters(rfVehicleInfo.Pos.Z));
            ComputeDistanceToPlayer(_lastPlayer, driverInfo, rfData);
            return driverInfo;
        }

        internal static void ComputeDistanceToPlayer(DriverInfo player, DriverInfo driverInfo, RfShared rfShared)
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

            double trackLength = rfShared.LapDist;
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

        internal void FillSessionInfo(RfShared data, SimulatorDataSet simData)
        {
            // Timing
            simData.SessionInfo.SessionTime = TimeSpan.FromSeconds(data.CurrentET);
            simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(data.LapDist);
            simData.SessionInfo.TrackInfo.TrackName = data.TrackName.FromArray();
            simData.SessionInfo.TrackInfo.TrackLayoutName = string.Empty;
            simData.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(data.AmbientTemp);
            simData.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(data.TrackTemp);

            if (data.TrackTemp == 0 && data.Session == 0 && data.GamePhase == 0
                && string.IsNullOrEmpty(simData.SessionInfo.TrackInfo.TrackName)
                && string.IsNullOrEmpty(data.VehicleName.FromArray()) && data.LapDist == 0)
            {
                simData.SessionInfo.SessionType = SessionType.Na;
                simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                return;
            }

            switch ((RfSessionType)data.Session)
            {
                case RfSessionType.NA:
                    simData.SessionInfo.SessionType = SessionType.Na;
                    break;
                case RfSessionType.Practice1:
                case RfSessionType.Practice2:
                case RfSessionType.Practice3:
                case RfSessionType.TestDay:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
                case RfSessionType.Qualification:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case RfSessionType.Race3:
                case RfSessionType.WarmUp:
                    simData.SessionInfo.SessionType = SessionType.WarmUp;
                    break;
                case RfSessionType.Race1:
                case RfSessionType.Race2:
                case RfSessionType.Race4:
                case RfSessionType.Race5:
                case RfSessionType.Race6:
                    simData.SessionInfo.SessionType = SessionType.Race;
                    break;
                default:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
            }

            switch ((RfGamePhase)data.GamePhase)
            {
                case RfGamePhase.Garage:
                    break;
                case RfGamePhase.WarmUp:
                case RfGamePhase.GridWalk:
                case RfGamePhase.Formation:
                case RfGamePhase.Countdown:
                    simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                    break;
                case RfGamePhase.SessionStopped:
                case RfGamePhase.GreenFlag:
                case RfGamePhase.FullCourseYellow:
                    simData.SessionInfo.SessionPhase = SessionPhase.Green;
                    break;
                case RfGamePhase.SessionOver:
                    simData.SessionInfo.SessionPhase = SessionPhase.Checkered;
                    break;
            }

            simData.SessionInfo.IsActive = simData.SessionInfo.SessionType != SessionType.Na;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (data.EndET > 0)
            {
                simData.SessionInfo.SessionLengthType = simData.SessionInfo.SessionType != SessionType.Race ? SessionLengthType.Time : SessionLengthType.TimeWithExtraLap;
                simData.SessionInfo.SessionTimeRemaining =
                    data.EndET - data.CurrentET > 0 ? data.EndET - data.CurrentET : 0;
            }
            else
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Laps;
                simData.SessionInfo.TotalNumberOfLaps = data.MaxLaps;
            }
        }
    }
}
namespace SecondMonitor.F12020Connector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using DataModels;
    using DataModels.Enums;
    using Foundation.Connectors;
    using PluginsConfiguration.Common.DataModel;

    using SecondMonitor.DataModel.Extensions;

    internal class F12020DataConvertor : AbstractDataConvertor
    {
        private readonly F12019Configuration _f1Configuration;
        public const string ConnectorName = SimulatorsNameMap.F12020;
        private readonly float[] _lapTimeCache;
        private readonly float[] _sector2TimeCache;
        private DriverInfo _lastPlayer;
        private double _layoutLength;
        private int _playerDataIndex;
        private float _maxStoreEnergy;

        internal F12020DataConvertor(F12019Configuration f1Configuration)
        {
            _f1Configuration = f1Configuration;
            _lapTimeCache = new float[22];
            _sector2TimeCache = new float[22];
            _maxStoreEnergy = 1;
        }

        internal void Reset()
        {
            _maxStoreEnergy = 1;
        }

        internal SimulatorDataSet ConvertData(AllPacketsComposition data)
        {
            SimulatorDataSet simData = new SimulatorDataSet(ConnectorName)
            {
                SimulatorSourceInfo =
                {
                    HasLapTimeInformation = true,
                    SectorTimingSupport = DataInputSupport.Full,
                    AIInstantFinish = true,
                    GapInformationProvided = GapInformationKind.None,
                    SimNotReportingEndOfOutLapCorrectly = true,
                    HasRewindFunctionality = true,
                    OverrideBestLap = true,
                    TelemetryInfo =
                    {
                        ContainsOptimalTemperatures = true,
                        ContainsSuspensionTravel = true,
                        ContainsSuspensionVelocity = true,
                    }
                }
            };

            FillSessionInfo(data, simData);

            //Add Flags Info
            AddFlags(ref data.PacketSessionData, simData);
            AddDriversData(ref data, simData);

            var rawTelemetryData = data.PacketCarTelemetryData.CarTelemetryData[_playerDataIndex];
            var rawCarStatusInfo = data.PacketCarStatusData.CarStatusData[_playerDataIndex];
            var rawCarMotionData = data.PacketMotionData.CarMotionData[_playerDataIndex];

            FillPlayerCarInfo(ref rawTelemetryData, simData);

            // PEDAL INFO
            AddPedalInfo(ref rawTelemetryData, simData);

            // Add Engine Temperatures
            AddEngineTemperatures(ref rawTelemetryData, simData);

            // Brakes Info
            AddBrakesInfo(ref rawTelemetryData, simData);

            // Tyre Pressure Info
            AddTyresInfo(ref rawTelemetryData, ref rawCarStatusInfo, ref data.PacketMotionData, simData);

            // Acceleration
            AddAcceleration(ref rawCarMotionData, simData);

            //Add Additional Player Car Info
            AddPlayerCarInfo(ref rawCarStatusInfo, ref rawTelemetryData, simData);

            simData.SessionInfo.IsMultiClass = simData.DriversInfo?.Any(x => x != null && x.Position != x.PositionInClass) == true;

            return simData;
        }

        private static DriverFinishStatus TranslateFinishStatus(int resultStatus)
        {
            switch ((DriverResultKind)resultStatus)
            {
                case DriverResultKind.Inactive:
                    return DriverFinishStatus.Dns;
                case DriverResultKind.Active:
                    return DriverFinishStatus.None;
                case DriverResultKind.Finished:
                    return DriverFinishStatus.Finished;
                case DriverResultKind.Disqualified:
                    return DriverFinishStatus.Dnq;
                case DriverResultKind.Retired:
                case DriverResultKind.NotClassified:
                    return DriverFinishStatus.Dnf;
                default:
                    return DriverFinishStatus.Na;
            }
        }

        private static void FillDrsData(ref CarStatusData carStatusData, ref CarTelemetryData rawCarTelemetryData, CarInfo playerCar)
        {
            DrsSystem drsSystem = playerCar.DrsSystem;

            drsSystem.DrsActivationLeft = -1;
            if (rawCarTelemetryData.Drs == 1)
            {
                drsSystem.DrsStatus = DrsStatus.InUse;
                return;
            }

            switch ((int)carStatusData.DrsAllowed)
            {
                case -1:
                case 0:
                    drsSystem.DrsStatus = DrsStatus.NotEquipped;
                    return;
                case 1:
                    drsSystem.DrsStatus = DrsStatus.Available;
                    break;
            }
        }

        private static void AddAcceleration(ref CarMotionData data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinG = data.GForceLateral;
            simData.PlayerInfo.CarInfo.Acceleration.YinG = data.GForceVertical;
            simData.PlayerInfo.CarInfo.Acceleration.ZinG = data.GForceLongitudinal;
        }

        private static void AddTyresInfo(ref CarTelemetryData rawTelemetryData, ref CarStatusData rawCarStatusData, ref PacketMotionData rawMotionData, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.RearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear = rawCarStatusData.TyresWear[TyreIndices.FrontLeft] / 100.0;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear = rawCarStatusData.TyresWear[TyreIndices.FrontRight] / 100.0;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear = rawCarStatusData.TyresWear[TyreIndices.RearLeft] / 100.0;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear = rawCarStatusData.TyresWear[TyreIndices.RearRight] / 100.0;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = rawMotionData.WheelSpeed[TyreIndices.FrontLeft];
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = rawMotionData.WheelSpeed[TyreIndices.FrontRight];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = rawMotionData.WheelSpeed[TyreIndices.RearLeft];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = rawMotionData.WheelSpeed[TyreIndices.RearRight];

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromCentimeters(rawMotionData.SuspensionPosition[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromCentimeters(rawMotionData.SuspensionPosition[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromCentimeters(rawMotionData.SuspensionPosition[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromCentimeters(rawMotionData.SuspensionPosition[TyreIndices.RearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionVelocity = Velocity.FromMs(rawMotionData.SuspensionVelocity[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionVelocity = Velocity.FromMs(rawMotionData.SuspensionVelocity[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionVelocity = Velocity.FromMs(rawMotionData.SuspensionVelocity[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionVelocity = Velocity.FromMs(rawMotionData.SuspensionVelocity[TyreIndices.RearRight]);

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.FrontLeft]);

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.FrontRight]);

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.RearLeft]);

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.RearRight]);

            // Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(rawCarStatusData.FuelCapacity);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(rawCarStatusData.FuelInTank);
        }

        private static void AddBrakesInfo(ref CarTelemetryData data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.BrakesTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.IdealQuantityWindow = Temperature.FromCelsius(200);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.IdealQuantity = Temperature.FromCelsius(600);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.BrakesTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.IdealQuantityWindow = Temperature.FromCelsius(200);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.IdealQuantity = Temperature.FromCelsius(600);

            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.BrakesTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.IdealQuantityWindow = Temperature.FromCelsius(200);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.IdealQuantity = Temperature.FromCelsius(600);

            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.BrakesTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.IdealQuantityWindow = Temperature.FromCelsius(200);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.IdealQuantity = Temperature.FromCelsius(600);
        }

        private static void AddEngineTemperatures(ref CarTelemetryData data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(data.EngineTemperature);
            simData.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature = simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature;
        }

        private static void AddWheelInfo(DriverInfo driverInfo, CarStatusData rawCarStatus)
        {
            string tyreType = TranslationTable.GetTyreCompound(rawCarStatus.TyreVisualCompound);
            driverInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = tyreType;
            driverInfo.CarInfo.WheelsInfo.FrontRight.TyreType = tyreType;

            driverInfo.CarInfo.WheelsInfo.RearLeft.TyreType = tyreType;
            driverInfo.CarInfo.WheelsInfo.RearRight.TyreType = tyreType;

            string tyreVisualType = TranslationTable.GetTyreVisualCompound(rawCarStatus.TyreVisualCompound);
            driverInfo.CarInfo.WheelsInfo.FrontLeft.TyreVisualType = tyreVisualType;
            driverInfo.CarInfo.WheelsInfo.FrontRight.TyreVisualType = tyreVisualType;

            driverInfo.CarInfo.WheelsInfo.RearLeft.TyreVisualType = tyreVisualType;
            driverInfo.CarInfo.WheelsInfo.RearRight.TyreVisualType = tyreVisualType;
        }

        private static void AddPedalInfo(ref CarTelemetryData rawData, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = rawData.Throttle;
            simData.InputInfo.BrakePedalPosition = rawData.Brake;
            simData.InputInfo.ClutchPedalPosition = rawData.Clutch / 100.0;
            simData.InputInfo.SteeringInput = rawData.Steer;
            simData.InputInfo.WheelAngle = 180 * rawData.Steer;
            simData.InputInfo.WheelAngleFilled = true;
        }

        private void FillBoostData(ref CarStatusData rawCarStatusData, CarInfo playerCar)
        {
            _maxStoreEnergy = Math.Max(_maxStoreEnergy, rawCarStatusData.ErsStoreEnergy);
            BoostSystem boostSystem = playerCar.BoostSystem;
            boostSystem.ActivationsRemaining = (int)((rawCarStatusData.ErsStoreEnergy / _maxStoreEnergy) * 100);
            if (rawCarStatusData.ErsDeployMode <= 1)
            {
                boostSystem.BoostStatus = BoostStatus.Available;
            }
            else if (rawCarStatusData.ErsDeployMode <= 3)
            {
                boostSystem.BoostStatus = BoostStatus.InUse;
            }
            else
            {
                boostSystem.BoostStatus = BoostStatus.Cooldown;
            }
        }

        private void ComputeDistanceToPlayer(DriverInfo player, DriverInfo driverInfo)
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

            double trackLength = _layoutLength;
            double playerLapDistance = player.LapDistance;

            double distanceToPlayer = playerLapDistance - driverInfo.LapDistance;
            if (distanceToPlayer < -(trackLength / 2))
            {
                distanceToPlayer = distanceToPlayer + trackLength;
            }

            if (distanceToPlayer > (trackLength / 2))
            {
                distanceToPlayer = distanceToPlayer - trackLength;
            }

            driverInfo.DistanceToPlayer = distanceToPlayer;
        }

        private void AddDriversData(ref AllPacketsComposition rawData, SimulatorDataSet data)
        {
            int numOfCars = data.SessionInfo.SessionType == SessionType.Race ? 22 : rawData.PacketParticipantsData.MNumActiveCars;

            List<DriverInfo> driverInfos = new List<DriverInfo>(22);
            DriverInfo playersInfo = null;

            for (int i = 0; i < numOfCars; i++)
            {
                var rawDriverData = rawData.PacketParticipantsData.MParticipants[i];
                string driverName = rawDriverData.Name.FromArray();
                if (string.IsNullOrWhiteSpace(driverName))
                {
                    continue;
                }

                DriverInfo driverInfo = CreateDriverInfo(rawData, driverName, i, data);
                if (driverInfo.IsPlayer)
                {
                    playersInfo = driverInfo;
                }

                AddWheelInfo(driverInfo, rawData.PacketCarStatusData.CarStatusData[i]);
                driverInfos.Add(driverInfo);
                if (driverInfo.Position == 1)
                {
                    data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                    data.LeaderInfo = driverInfo;
                }

                AddLappingInformation(data, driverInfo);
            }

            if (playersInfo != null)
            {
                data.PlayerInfo = playersInfo;
                _lastPlayer = playersInfo;
            }

            data.DriversInfo = driverInfos.ToArray();
        }

        private void AddLappingInformation(SimulatorDataSet data, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType != SessionType.Race || _lastPlayer == null || _lastPlayer.CompletedLaps == 0)
            {
                return;
            }

            driverInfo.IsBeingLappedByPlayer = driverInfo.TotalDistance < (_lastPlayer.TotalDistance - (_layoutLength * 0.5));
            driverInfo.IsLappingPlayer = _lastPlayer.TotalDistance < (driverInfo.TotalDistance - (_layoutLength * 0.5));
        }

        private DriverInfo CreateDriverInfo(AllPacketsComposition rawData, string driverName, int driverIndex, SimulatorDataSet data)
        {
            var rawDriverData = rawData.PacketParticipantsData.MParticipants[driverIndex];
            var rawDriverLapInfo = rawData.PacketLapData.LapData[driverIndex];
            var rawCarMotionData = rawData.PacketMotionData.CarMotionData[driverIndex];
            DriverInfo driverInfo = new DriverInfo
            {
                DriverSessionId = driverName,
                CompletedLaps = rawDriverLapInfo.CurrentLapNum - 1,
                CarName = string.Empty,
                InPits = rawDriverLapInfo.PitStatus != 0 || rawDriverLapInfo.DriverStatus == 0,
                IsPlayer = driverIndex == rawData.PacketCarTelemetryData.Header.PlayerCarIndex,
                Position = rawDriverLapInfo.CarPosition,
                PositionInClass = rawDriverLapInfo.CarPosition,
                Speed = Velocity.FromKph(rawData.PacketCarTelemetryData.CarTelemetryData[driverIndex].Speed),
                LapDistance = rawDriverLapInfo.LapDistance >= 0 ? rawDriverLapInfo.LapDistance : rawData.PacketSessionData.TrackLength + rawDriverLapInfo.LapDistance,
                TotalDistance = rawDriverLapInfo.TotalDistance
            };
            driverInfo.DriverShortName = driverInfo.DriverLongName = driverInfo.DriverSessionId;
            driverInfo.CarName = rawDriverData.TeamId == 255 ? _f1Configuration.MyTeamName : TranslationTable.GetCarName(rawDriverData.TeamId);
            driverInfo.CarClassName = TranslationTable.GetClass(rawData.PacketSessionData.Formula);
            driverInfo.CarClassId = driverInfo.CarClassName;
            driverInfo.FinishStatus = rawData.AdditionalData.RetiredDrivers[driverIndex] ? DriverFinishStatus.Dnf : TranslateFinishStatus(rawDriverLapInfo.ResultStatus);
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(rawCarMotionData.WorldPositionX), Distance.FromMeters(rawCarMotionData.WorldPositionY), Distance.FromMeters(rawCarMotionData.WorldPositionZ));
            driverInfo.CurrentLapValid = rawDriverLapInfo.CurrentLapInvalid == 0 && rawDriverLapInfo.DriverStatus != 2 && rawDriverLapInfo.DriverStatus != 3;

            if (data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector1) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector2) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector3))
            {
                driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
            }

            ComputeDistanceToPlayer(_lastPlayer, driverInfo);
            FillTimingInfo(driverInfo, rawDriverLapInfo, driverIndex);
            return driverInfo;
        }

        private void FillTimingInfo(DriverInfo driverInfo, LapData rawLapData, int driverIndex)
        {
            driverInfo.Timing.CurrentSector = rawLapData.Sector + 1;
            driverInfo.Timing.LastSector1Time = TimeSpan.FromSeconds(driverInfo.Timing.CurrentSector == 1 ? rawLapData.CurrentLapTime : rawLapData.Sector1TimeInMs / 1000.0);
            driverInfo.Timing.LastSector2Time = TimeSpan.FromSeconds(rawLapData.CurrentLapTime - (rawLapData.Sector1TimeInMs / 1000.0));

            if (driverInfo.Timing.CurrentSector == 3)
            {
                _sector2TimeCache[driverIndex] = (float)(rawLapData.Sector2TimeInMs / 1000.0);
                _lapTimeCache[driverIndex] = rawLapData.CurrentLapTime;
            }

            driverInfo.Timing.CurrentLapTime = TimeSpan.FromSeconds(rawLapData.CurrentLapTime);
            driverInfo.Timing.LastSector3Time = TimeSpan.FromSeconds(driverInfo.Timing.CurrentSector == 3 ? rawLapData.CurrentLapTime - (rawLapData.Sector2TimeInMs / 1000.0) : _lapTimeCache[driverIndex] - _sector2TimeCache[driverIndex]);
            driverInfo.Timing.LastLapTime = TimeSpan.FromSeconds(driverInfo.Timing.CurrentSector == 3 ? rawLapData.LastLapTime : _lapTimeCache[driverIndex]);
            driverInfo.Timing.BestLapTime = TimeSpan.FromSeconds(rawLapData.BestLapTime);
        }

        private void AddPlayerCarInfo(ref CarStatusData rawCarStatusData, ref CarTelemetryData rawCarTelemetryData, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;

            playerCar.CarDamageInformation.Bodywork.MediumDamageThreshold = 0.1;
            playerCar.CarDamageInformation.Engine.HeavyDamageThreshold = 0.9;
            playerCar.CarDamageInformation.Transmission.HeavyDamageThreshold = 0.75;
            playerCar.CarDamageInformation.Engine.MediumDamageThreshold = 0.75;
            playerCar.CarDamageInformation.Transmission.MediumDamageThreshold = 0.9;
            /*playerCar.CarDamageInformation.Suspension.Damage = rawCarStatusData.MTyresDamage.Max(x => x) / 100.0;*/
            playerCar.CarDamageInformation.Bodywork.Damage = Math.Max(rawCarStatusData.RearWingDamage, Math.Max(rawCarStatusData.FrontLeftWingDamage, rawCarStatusData.FrontRightWingDamage)) / 100.0;
            playerCar.CarDamageInformation.Engine.Damage = rawCarStatusData.EngineDamage / 100.0;
            playerCar.CarDamageInformation.Transmission.Damage = rawCarStatusData.GearBoxDamage / 100.0;

            playerCar.SpeedLimiterEngaged = rawCarStatusData.PitLimiterStatus == 1;

            /*playerCar.WorldOrientation = new Orientation()
            {
                Roll = Angle.GetFromRadians(rawCarStatusData.CarOrientation.Roll),
                Pitch = Angle.GetFromRadians(rawCarStatusData.CarOrientation.Pitch),
                Yaw = Angle.GetFromRadians(-rawCarStatusData.CarOrientation.Yaw),
            };*/

            FillDrsData(ref rawCarStatusData, ref rawCarTelemetryData, playerCar);
            FillBoostData(ref rawCarStatusData, playerCar);
        }

        private void AddFlags(ref PacketSessionData data, SimulatorDataSet simData)
        {
            if (data.SafetyCarStatus == 2)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.VirtualSafetyCar;
                return;
            }

            if (data.SafetyCarStatus == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.SafetyCar;
                return;
            }

            foreach (var zoneWithFlag in data.MarshalZones.Take(data.NumMarshalZones).Where(x => x.ZoneFlag == 3))
            {
                if (zoneWithFlag.ZoneStart < 0.33)
                {
                    simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
                    continue;
                }

                if (zoneWithFlag.ZoneStart < 0.66)
                {
                    simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
                    continue;
                }

                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
            }
        }

        private void FillPlayerCarInfo(ref CarTelemetryData rawTelemetryData, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.EngineRpm = rawTelemetryData.EngineRpm;
            switch (rawTelemetryData.Gear)
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
                    simData.PlayerInfo.CarInfo.CurrentGear = rawTelemetryData.Gear.ToString();
                    break;
            }
        }

        private void FillSessionInfo(AllPacketsComposition data, SimulatorDataSet simData)
        {
            // Timing
            simData.SessionInfo.SessionTime = TimeSpan.FromSeconds(data.PacketCarTelemetryData.Header.SessionTime);
            _layoutLength = data.PacketSessionData.TrackLength;
            _playerDataIndex = data.PacketSessionData.Header.PlayerCarIndex;
            simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(_layoutLength);
            simData.SessionInfo.IsActive = true;
            simData.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(data.PacketSessionData.AirTemperature);
            simData.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(data.PacketSessionData.TrackTemperature);
            if (data.PacketSessionData.Weather == 3)
            {
                simData.SessionInfo.WeatherInfo.RainIntensity = 30;
            }
            else if (data.PacketSessionData.Weather == 4)
            {
                simData.SessionInfo.WeatherInfo.RainIntensity = 60;
            }
            else if (data.PacketSessionData.Weather == 5)
            {
                simData.SessionInfo.WeatherInfo.RainIntensity = 100;
            }

            switch ((SessionKind)data.PacketSessionData.SessionType)
            {
                case SessionKind.TimeTrial:
                case SessionKind.Practice1:
                case SessionKind.Practice2:
                case SessionKind.Practice3:
                case SessionKind.ShortPractice:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
                case SessionKind.Qualification1:
                case SessionKind.Qualification2:
                case SessionKind.Qualification3:
                case SessionKind.ShortQualification:
                case SessionKind.OnlineQualification:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case SessionKind.Race:
                case SessionKind.Race2:
                    simData.SessionInfo.SessionType = SessionType.Race;
                    break;
                default:
                    simData.SessionInfo.SessionType = SessionType.Na;
                    break;
            }

            simData.SessionInfo.SessionPhase = SessionPhase.Green;

            simData.SessionInfo.TrackInfo.TrackName = TranslationTable.GetTrackName(data.PacketSessionData.TrackId);
            simData.SessionInfo.TrackInfo.TrackLayoutName = string.Empty;

            if (data.PacketSessionData.TotalLaps > 2 && simData.SessionInfo.SessionType == SessionType.Race)
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Laps;
                simData.SessionInfo.TotalNumberOfLaps = data.PacketSessionData.TotalLaps;
            }
            else
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Time;
                simData.SessionInfo.SessionTimeRemaining = data.PacketSessionData.SessionTimeLeft;
            }
        }
    }
}
namespace SecondMonitor.F12024Connector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.F12024Connector.DataModels;
    using SecondMonitor.F12024Connector.DataModels.Enums;
    using SecondMonitor.Foundation.Connectors;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;

    internal class F12024DataConvertor : AbstractDataConvertor
    {
        public const string F1Class = "Formula 1";
        public const string F2Class = "Formula 2";

        private readonly F12019Configuration _f1Configuration;
        public const string ConnectorName = SimulatorsNameMap.F12024;
        private readonly uint[] _lapTimeCache;
        private readonly ushort[] _sector2TimeCache;
        private DriverInfo _lastPlayer;
        private double _layoutLength;
        private int _playerDataIndex;
        private float _maxStoreEnergy;

        internal F12024DataConvertor(F12019Configuration f1Configuration)
        {
            _f1Configuration = f1Configuration;
            _lapTimeCache = new uint[22];
            _sector2TimeCache = new ushort[22];
            _maxStoreEnergy = 4000000;
        }

        internal void Reset()
        {
            _maxStoreEnergy = 4000000;
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
            AddFlags(ref data.PacketSessionData, ref data.AdditionalData.LastSafetyCarUpdate, simData);
            AddDriversData(ref data, simData);

            var rawTelemetryData = data.PacketCarTelemetryData.CarTelemetryData[_playerDataIndex];
            var rawCarStatusInfo = data.PacketCarStatusData.CarStatusData[_playerDataIndex];
            var rawCarMotionData = data.PacketMotionData.CarMotionData[_playerDataIndex];
            var rawCarDamageData = data.PacketCarDamageData.CarDamageData[_playerDataIndex];

            FillPlayerCarInfo(ref rawTelemetryData, simData);

            // PEDAL INFO
            AddPedalInfo(ref rawTelemetryData, simData);

            // Add Engine Temperatures
            AddEngineTemperatures(ref rawTelemetryData, simData);

            // Brakes Info
            AddBrakesInfo(ref rawTelemetryData, simData);

            // Tyre Pressure Info
            AddTyresInfo(ref rawTelemetryData, ref rawCarStatusInfo, ref rawCarDamageData, ref data.PacketMotionExData, simData);

            // Acceleration
            AddAcceleration(ref rawCarMotionData, simData);

            //Add Additional Player Car Info
            AddPlayerCarInfo(ref rawCarStatusInfo, ref rawTelemetryData, ref rawCarDamageData, simData);

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

        private static void AddTyresInfo(ref CarTelemetryData rawTelemetryData, ref CarStatusData rawCarStatusData, ref CarDamageData rawCarDamageData, ref PacketMotionExData packetMotionExData, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity = Pressure.FromPsi(rawTelemetryData.TyresPressure[TyreIndices.RearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear = rawCarDamageData.TyresWear[TyreIndices.FrontLeft] / 100.0;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear = rawCarDamageData.TyresWear[TyreIndices.FrontRight] / 100.0;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear = rawCarDamageData.TyresWear[TyreIndices.RearLeft] / 100.0;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear = rawCarDamageData.TyresWear[TyreIndices.RearRight] / 100.0;
            
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = packetMotionExData.WheelSpeed[TyreIndices.FrontLeft];
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = packetMotionExData.WheelSpeed[TyreIndices.FrontRight];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = packetMotionExData.WheelSpeed[TyreIndices.RearLeft];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = packetMotionExData.WheelSpeed[TyreIndices.RearRight];

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromCentimeters(packetMotionExData.SuspensionPosition[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromCentimeters(packetMotionExData.SuspensionPosition[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromCentimeters(packetMotionExData.SuspensionPosition[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromCentimeters(packetMotionExData.SuspensionPosition[TyreIndices.RearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionVelocity = Velocity.FromMs(packetMotionExData.SuspensionVelocity[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionVelocity = Velocity.FromMs(packetMotionExData.SuspensionVelocity[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionVelocity = Velocity.FromMs(packetMotionExData.SuspensionVelocity[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionVelocity = Velocity.FromMs(packetMotionExData.SuspensionVelocity[TyreIndices.RearRight]);

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.FrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight = Distance.FromCentimeters(packetMotionExData.FrontAeroHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakesDamage.Damage = rawCarDamageData.BrakesDamage[TyreIndices.FrontLeft] / 100.0;

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.FrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight = Distance.FromCentimeters(packetMotionExData.FrontAeroHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakesDamage.Damage = rawCarDamageData.BrakesDamage[TyreIndices.FrontRight] / 100.0;

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.RearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight = Distance.FromCentimeters(packetMotionExData.RearAeroHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakesDamage.Damage = rawCarDamageData.BrakesDamage[TyreIndices.RearLeft] / 100.0;

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.IdealQuantity = Temperature.FromCelsius(85);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.IdealQuantityWindow = Temperature.FromCelsius(20);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresSurfaceTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(rawTelemetryData.TyresInnerTemperature[TyreIndices.RearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight = Distance.FromCentimeters(packetMotionExData.RearAeroHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakesDamage.Damage = rawCarDamageData.BrakesDamage[TyreIndices.RearRight] / 100.0;

            // Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(rawCarStatusData.FuelCapacity);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(rawCarStatusData.FuelInTank);

            simData.PlayerInfo.CarInfo.FrontHeight = Distance.FromCentimeters(packetMotionExData.FrontAeroHeight);
            simData.PlayerInfo.CarInfo.RearHeight = Distance.FromCentimeters(packetMotionExData.RearAeroHeight);
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

        private void FillBoostData(ref CarStatusData rawCarStatusData, DriverInfo player)
        {
            // Réglages en roulant (SecondMonitor multiplateforme) : répartition, TC, ABS, mélange carburant (cartographie).
            CarControls controls = player.CarInfo.Controls;
            controls.Available = true;
            controls.FrontBrakeBiasPct = rawCarStatusData.FrontBrakeBias > 0 ? rawCarStatusData.FrontBrakeBias : null;
            controls.Tc = rawCarStatusData.TractionControl;
            controls.TcMax = 2;
            controls.Abs = rawCarStatusData.AntiLockBrakes;
            controls.AbsMax = 1;
            controls.MotorMap = rawCarStatusData.FuelMix;
            controls.MotorMapMax = 3;
            controls.SpeedLimiterAvailable = true;
            if (player.CarClassId != F1Class)
            {
                return;
            }

            controls.ElectricMotorState = rawCarStatusData.EnginePowerMGUK > 1000 ? 2 : rawCarStatusData.EnginePowerMGUK < -1000 ? 3 : 1;
            if (_maxStoreEnergy > 0)
            {
                controls.BatteryPct = Math.Round(rawCarStatusData.ErsStoreEnergy / 40000.0, 1); // 4 MJ = 100 %
            }

            CarInfo playerCar = player.CarInfo;
            _maxStoreEnergy = Math.Max(_maxStoreEnergy, rawCarStatusData.ErsStoreEnergy);
            HybridSystem hybridSystem = playerCar.HybridSystem;
            hybridSystem.RemainingChargePercentage = (rawCarStatusData.ErsStoreEnergy / _maxStoreEnergy);
            hybridSystem.HybridSystemMode = rawCarStatusData.ErsDeployMode switch
            {
                // ERS deployment mode, 0 = none, 1 = low, 2 = medium, 3 = high, 4 = overtake, 5 = hotlap
                0 => HybridSystemMode.Build,
                1 => HybridSystemMode.Balanced,
                2 => HybridSystemMode.Attack,
                3 or 4 or 5 => HybridSystemMode.Qualification,
                _ => hybridSystem.HybridSystemMode
            };

            hybridSystem.RemainingPerLapChargePercentage = (rawCarStatusData.ErsDeployedThisLap / 4000000.0);
            hybridSystem.HarvestedThisLapPercentage = (rawCarStatusData.ErsHarvestedThisLapMguk + rawCarStatusData.ErsHarvestedThisLapMguh) / 4000000.0;
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
                DriverSessionId = TranslationTable.GetDriverName(driverName),
                CompletedLaps = rawDriverLapInfo.CurrentLapNum - 1,
                CarName = string.Empty,
                InPits = rawDriverLapInfo.PitStatus != 0 || rawDriverLapInfo.DriverStatus == 0,
                IsPlayer = driverIndex == rawData.PacketCarTelemetryData.Header.PlayerCarIndex,
                Position = rawDriverLapInfo.CarPosition,
                PositionInClass = rawDriverLapInfo.CarPosition,
                Speed = Velocity.FromKph(rawData.PacketCarTelemetryData.CarTelemetryData[driverIndex].Speed),
                LapDistance = rawDriverLapInfo.LapDistance >= 0 ? rawDriverLapInfo.LapDistance : rawData.PacketSessionData.TrackLength + rawDriverLapInfo.LapDistance,
                TotalDistance = rawDriverLapInfo.TotalDistance,
                CarRaceNumber = rawDriverData.RaceNumber,
                TeamName = TranslationTable.GetTeamName(rawDriverData.TeamId),
            };
            driverInfo.DriverShortName = driverInfo.DriverLongName = driverInfo.DriverSessionId;
            driverInfo.CarName = rawDriverData.MyTeam == 1 ? _f1Configuration.MyTeamName : TranslationTable.GetCarName(rawDriverData.TeamId);
            driverInfo.CarClassName = rawDriverData.TeamId > 104 ? F2Class : F1Class;
            driverInfo.CarClassId = driverInfo.CarClassName;
            driverInfo.FinishStatus = rawData.AdditionalData.RetiredDrivers[driverIndex] ? DriverFinishStatus.Dnf : TranslateFinishStatus(rawDriverLapInfo.ResultStatus);
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(rawCarMotionData.WorldPositionX), Distance.FromMeters(rawCarMotionData.WorldPositionY), Distance.FromMeters(rawCarMotionData.WorldPositionZ));
            driverInfo.CurrentLapValid = rawDriverLapInfo.CurrentLapInvalid == 0 && rawDriverLapInfo.DriverStatus != 2 && rawDriverLapInfo.DriverStatus != 3;

            if (data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector1) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector2) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector3))
            {
                driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
            }

            ComputeDistanceToPlayer(_lastPlayer, driverInfo);

            //There is an issue in F1 2021, at the beginning of each session the history data contain previous session info
            if (data.SessionInfo.SessionTime.TotalSeconds > 3)
            {
                FillTimingInfo(driverInfo, rawDriverLapInfo, driverIndex, rawData.PacketSessionHistoryData[driverIndex]);
            }

            return driverInfo;
        }

        private void FillTimingInfo(DriverInfo driverInfo, LapData rawLapData, int driverIndex, PacketSessionHistoryData packetSessionHistoryData)
        {
            driverInfo.Timing.CurrentSector = rawLapData.Sector + 1;
            driverInfo.Timing.LastSector1Time = TimeSpan.FromMilliseconds(driverInfo.Timing.CurrentSector == 1 ? rawLapData.CurrentLapTimeMs : rawLapData.Sector1TimeInMs);
            driverInfo.Timing.LastSector2Time = TimeSpan.FromMilliseconds(rawLapData.CurrentLapTimeMs - rawLapData.Sector1TimeInMs);

            if (driverInfo.Timing.CurrentSector == 3)
            {
                _sector2TimeCache[driverIndex] = rawLapData.Sector2TimeInMs;
                _lapTimeCache[driverIndex] = rawLapData.CurrentLapTimeMs;
            }

            driverInfo.Timing.CurrentLapTime = TimeSpan.FromMilliseconds(rawLapData.CurrentLapTimeMs);
            driverInfo.Timing.LastSector3Time = TimeSpan.FromMilliseconds(driverInfo.Timing.CurrentSector == 3 ? rawLapData.CurrentLapTimeMs - rawLapData.Sector2TimeInMs : _lapTimeCache[driverIndex] - _sector2TimeCache[driverIndex]);
            driverInfo.Timing.LastLapTime = TimeSpan.FromMilliseconds(rawLapData.LastLapTimeMs);

            uint timeFromRawData = packetSessionHistoryData.BestLapTimeLapNum != 0 && packetSessionHistoryData.LapHistoryData != null ?
                packetSessionHistoryData.LapHistoryData[packetSessionHistoryData.BestLapTimeLapNum - 1].LapTimeInMS : 0;

            if (timeFromRawData > 0)
            {
                driverInfo.Timing.BestLapTime = TimeSpan.FromMilliseconds(timeFromRawData);
                driverInfo.Timing.BestLapTimeNumber = packetSessionHistoryData.BestLapTimeLapNum;
            }
        }

        private void AddPlayerCarInfo(ref CarStatusData rawCarStatusData, ref CarTelemetryData rawCarTelemetryData, ref CarDamageData rawCarDamageData, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;

            playerCar.CarDamageInformation.Bodywork.MediumDamageThreshold = 0.1;
            playerCar.CarDamageInformation.Engine.HeavyDamageThreshold = 0.9;
            playerCar.CarDamageInformation.Transmission.HeavyDamageThreshold = 0.75;
            playerCar.CarDamageInformation.Engine.MediumDamageThreshold = 0.75;
            playerCar.CarDamageInformation.Transmission.MediumDamageThreshold = 0.9;
            /*playerCar.CarDamageInformation.Suspension.Damage = rawCarStatusData.MTyresDamage.Max(x => x) / 100.0;*/
            playerCar.CarDamageInformation.Bodywork.Damage = Math.Max(rawCarDamageData.FloorDamage,
                Math.Max(rawCarDamageData.DiffuserDamage,
                    Math.Max(rawCarDamageData.SidePodDamage,
                        Math.Max(rawCarDamageData.RearWingDamage,
                            Math.Max(rawCarDamageData.FrontLeftWingDamage, rawCarDamageData.FrontRightWingDamage))))) / 100.0;
            playerCar.CarDamageInformation.Engine.Damage = rawCarDamageData.EngineDamage / 100.0;
            playerCar.CarDamageInformation.Transmission.Damage = rawCarDamageData.GearBoxDamage / 100.0;

            // Détail des pièces (SecondMonitor multiplateforme) : F1 donne chaque élément aéro, l'usure de chaque partie moteur et les pannes.
            var parts = playerCar.CarDamageInformation.Parts;
            parts.Clear();
            parts.Add(new DamagePart("front-wing-left", "aero", rawCarDamageData.FrontLeftWingDamage / 100.0));
            parts.Add(new DamagePart("front-wing-right", "aero", rawCarDamageData.FrontRightWingDamage / 100.0));
            parts.Add(new DamagePart("rear-wing", "aero", rawCarDamageData.RearWingDamage / 100.0));
            parts.Add(new DamagePart("floor", "aero", rawCarDamageData.FloorDamage / 100.0));
            parts.Add(new DamagePart("diffuser", "aero", rawCarDamageData.DiffuserDamage / 100.0));
            parts.Add(new DamagePart("sidepod", "aero", rawCarDamageData.SidePodDamage / 100.0));
            parts.Add(new DamagePart("gearbox", "gearbox", rawCarDamageData.GearBoxDamage / 100.0));
            parts.Add(new DamagePart("engine", "engine", rawCarDamageData.EngineDamage / 100.0));
            parts.Add(new DamagePart("engine-ice-wear", "engine", rawCarDamageData.EngineICEWear / 100.0));
            parts.Add(new DamagePart("engine-mguh-wear", "engine", rawCarDamageData.EngineMGUHWear / 100.0));
            parts.Add(new DamagePart("engine-mguk-wear", "engine", rawCarDamageData.EngineMGUKWear / 100.0));
            parts.Add(new DamagePart("engine-es-wear", "engine", rawCarDamageData.EngineESWear / 100.0));
            parts.Add(new DamagePart("engine-ce-wear", "engine", rawCarDamageData.EngineCEWear / 100.0));
            parts.Add(new DamagePart("engine-tc-wear", "engine", rawCarDamageData.EngineTCWear / 100.0));
            string[] wheels = { "rl", "rr", "fl", "fr" }; // ordre F1 : arrière gauche, arrière droit, avant gauche, avant droit
            for (int i = 0; i < 4 && rawCarDamageData.TyresDamage != null && i < rawCarDamageData.TyresDamage.Length; i++)
            {
                parts.Add(new DamagePart($"tyre-{wheels[i]}", "tyres", rawCarDamageData.TyresDamage[i] / 100.0));
            }

            parts.Add(new DamagePart("drs-fault", "electronics", rawCarDamageData.DrsFault != 0 ? 1 : 0));
            parts.Add(new DamagePart("ers-fault", "electronics", rawCarDamageData.ErsFault != 0 ? 1 : 0));
            parts.Add(new DamagePart("engine-blown", "engine", rawCarDamageData.EngineBlown != 0 ? 1 : 0));
            parts.Add(new DamagePart("engine-seized", "engine", rawCarDamageData.EngineSeized != 0 ? 1 : 0));

            playerCar.SpeedLimiterEngaged = rawCarStatusData.PitLimiterStatus == 1;

            /*playerCar.WorldOrientation = new Orientation()
            {
                Roll = Angle.GetFromRadians(rawCarStatusData.CarOrientation.Roll),
                Pitch = Angle.GetFromRadians(rawCarStatusData.CarOrientation.Pitch),
                Yaw = Angle.GetFromRadians(-rawCarStatusData.CarOrientation.Yaw),
            };*/

            FillDrsData(ref rawCarStatusData, ref rawCarTelemetryData, playerCar);
            FillBoostData(ref rawCarStatusData, simData.PlayerInfo);
        }

        private void AddFlags(ref PacketSessionData data, ref SafetyCar safetyCar, SimulatorDataSet simData)
        {
            if (data.SafetyCarStatus == 2)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.VirtualSafetyCar;
                return;
            }

            if (data.SafetyCarStatus == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.SafetyCar;
                simData.SessionInfo.SafetyCarState = safetyCar.EventType switch
                {
                    0 => SafetyCarState.PitOpen,
                    1 => SafetyCarState.LightsOff,
                    2 => SafetyCarState.None,
                    _ => SafetyCarState.None,
                };

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
            simData.SessionInfo.SpectatingState = data.PacketSessionData.IsSpectating == 1 ? SpectatingState.Spectate : SpectatingState.Live;
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
                case SessionKind.SprintShootout1:
                case SessionKind.SprintShootout2:
                case SessionKind.SprintShootout3:
                case SessionKind.ShortSprintShootout:
                case SessionKind.OneShotSprintShootout:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case SessionKind.Race1:
                case SessionKind.Race2:
                case SessionKind.Race3:
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
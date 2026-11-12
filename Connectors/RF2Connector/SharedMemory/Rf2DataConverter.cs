namespace SecondMonitor.RF2Connector.SharedMemory
{
    using System;
    using System.Linq;

    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;

    using Foundation.Connectors;
    using Foundation.Connectors.Visitor;

    using rFactor2Data;

    using SecondMonitor.DataModel.Extensions;

    internal class Rf2DataConverter : AbstractDataConvertor
    {
        private const int MaxConsecutivePackagesIgnored = 200;
        private static readonly string[] classesWithHybrid = new string[] { "Formula E Gen3 2023" };
        private static readonly string[] classesWithElectricOnly = new string[] { "Formula E Gen3 2023" };
        private readonly SessionTimeInterpolator _sessionTimeInterpolator;

        private DriverInfo _lastPlayer = new();
        private int _lastPlayerId = -1;
        private rF2VehicleTelemetry _lastPlayerTelemetry;

        private int _currentlyIgnoredPackage;
        private bool _playerHasHybrid;
        private bool _playerIsElectricOnly;

        public Rf2DataConverter(SessionTimeInterpolator sessionTimeInterpolator)
        {
            _sessionTimeInterpolator = sessionTimeInterpolator;
        }

        public SimulatorDataSet CreateSimulatorDataSet(in Rf2FullData rfData)
        {
            try
            {
                SimulatorDataSet simData = new(SimulatorsNameMap.Rf2SimName)
                {
                    SimulatorSourceInfo =
                    {
                        GapInformationProvided = GapInformationKind.None,
                        HasLapTimeInformation = true,
                        SimNotReportingEndOfOutLapCorrectly = true,
                        InvalidateLapBySector = true,
                        SectorTimingSupport = DataInputSupport.Full,
                        TelemetryInfo = { ContainsSuspensionTravel = true, ContainsWheelRps = true, ContainsTyreLoad = true },
                    }
                };
                //simData.SimulatorSourceInfo.TelemetryInfo.ContainsSuspensionVelocity = true;
                /*                simData.SimulatorSourceInfo.TelemetryInfo.RequiresDistanceInterpolation = true;
                                simData.SimulatorSourceInfo.TelemetryInfo.RequiresPositionInterpolation = true;*/

                FillSessionInfo(rfData, simData);
                AddFlags(rfData, simData);
                AddDriversData(simData, rfData);

                if (_lastPlayerId == -1)
                {
                    return simData;
                }

                rF2VehicleTelemetry playerF2VehicleTelemetry = rfData.telemetry.mVehicles.FirstOrDefault(x => x.mID == _lastPlayerId && x.mElapsedTime > 0);

                //This is unreliable, it resets at long races
                /*if (playerF2VehicleTelemetry.mElapsedTime > 0)
                {
                    simData.SessionInfo.SessionTime = TimeSpan.FromSeconds(playerF2VehicleTelemetry.mElapsedTime);
                }*/

                if (playerF2VehicleTelemetry.mWheels == null && _lastPlayerTelemetry.mWheels != null)
                {
                    playerF2VehicleTelemetry = _lastPlayerTelemetry;
                }
                else
                {
                    _lastPlayerTelemetry = playerF2VehicleTelemetry;
                }

                if (playerF2VehicleTelemetry.mWheels == null)
                {
                    return simData;
                }

                FillPlayerCarInfo(playerF2VehicleTelemetry, simData);

                // PEDAL INFO
                AddPedalInfo(playerF2VehicleTelemetry, simData);

                // WaterSystemInfo
                AddWaterSystemInfo(playerF2VehicleTelemetry, simData);

                // OilSystemInfo
                AddOilSystemInfo(playerF2VehicleTelemetry, simData);

                // Brakes Info
                AddBrakesInfo(playerF2VehicleTelemetry, simData);

                // Tyre Pressure Info
                AddTyresAndFuelInfo(simData, playerF2VehicleTelemetry);

                // Acceleration
                AddAcceleration(simData, playerF2VehicleTelemetry);

                //Add Additional Player Car Info
                AddPlayerCarInfo(playerF2VehicleTelemetry, simData);

                _currentlyIgnoredPackage = 0;

                PopulateClassPositions(simData);

                FillSpectateInfo(simData, rfData);

                FillHybridData(simData, playerF2VehicleTelemetry);

                return simData;
            }
            catch (Exception ex)
            {
                _lastPlayerId = -1;
                _lastPlayer = new DriverInfo();
                throw new RF2InvalidPackageException(ex);
            }
        }

        private void FillHybridData(SimulatorDataSet simData, rF2VehicleTelemetry playerData)
        {
            if (!_playerHasHybrid || playerData.mBatteryChargeFraction < 0)
            {
                return;
            }

            HybridSystem hybridSystem = simData.PlayerInfo.CarInfo.HybridSystem;
            hybridSystem.RemainingChargePercentage = playerData.mBatteryChargeFraction;
            hybridSystem.HybridSystemMode = HybridSystemMode.Balanced;
            hybridSystem.IsElectricOnly = _playerIsElectricOnly;
        }

        private void FillSpectateInfo(SimulatorDataSet data, in Rf2FullData rfData)
        {
            if (_lastPlayerId == -1)
            {
                data.SessionInfo.SpectatingState = SpectatingState.Spectate;
            }

            data.SessionInfo.SpectatingState = rfData.scoring.mVehicles[_lastPlayerId].mControl != 3 ? SpectatingState.Live
                : SpectatingState.Spectate;
        }

        internal static DriverFinishStatus FromRfStatus(int finishStatus)
        {
            switch ((rFactor2Constants.rF2FinishStatus)finishStatus)
            {
                case rFactor2Constants.rF2FinishStatus.None:
                    return DriverFinishStatus.None;
                case rFactor2Constants.rF2FinishStatus.Dnf:
                    return DriverFinishStatus.Dnf;
                case rFactor2Constants.rF2FinishStatus.Dq:
                    return DriverFinishStatus.Dq;
                case rFactor2Constants.rF2FinishStatus.Finished:
                    return DriverFinishStatus.Finished;
                default:
                    return DriverFinishStatus.Na;
            }
        }

        private static void AddBrakesInfo(rF2VehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mBrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mBrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mBrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mBrakeTemp);
        }

        private static void AddOilSystemInfo(rF2VehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(playerVehicleTelemetry.mEngineOilTemp);
            simData.PlayerInfo.CarInfo.TurboPressure = Pressure.FromKiloPascals(playerVehicleTelemetry.mTurboBoostPressure / 1000);
        }

        private static void AddWaterSystemInfo(rF2VehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(playerVehicleTelemetry.mEngineWaterTemp);
        }

        private static void AddPedalInfo(rF2VehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = playerVehicleTelemetry.mUnfilteredThrottle;
            simData.InputInfo.BrakePedalPosition = playerVehicleTelemetry.mUnfilteredBrake;
            simData.InputInfo.ClutchPedalPosition = playerVehicleTelemetry.mUnfilteredClutch;
            simData.InputInfo.SteeringInput = playerVehicleTelemetry.mUnfilteredSteering;
        }

        private void AddPlayerCarInfo(rF2VehicleTelemetry data, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;

            int totalDent = data.mDentSeverity.Aggregate((x, y) => (byte)(x + y));
            int maxDent = data.mDentSeverity.Max();
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

            if (data.mOverheating == 1)
            {
                playerCar.CarDamageInformation.Engine.Damage = 1;
            }

            /*playerCar.WorldOrientation = new Orientation()
            {
                Yaw = Angle.GetFromRadians(Math.Atan2(data.mOri[2].x, data.mOri[2].z)),
            };*/

            playerCar.SpeedLimiterEngaged = data.mSpeedLimiter == 1;
            playerCar.FrontDownForce = Force.GetFromNewtons(data.mFrontDownforce);
            playerCar.RearDownForce = Force.GetFromNewtons(data.mRearDownforce);
            playerCar.OverallDownForce = Force.GetFromNewtons(data.mFrontDownforce + data.mRearDownforce);
        }

        private void AddFlags(in Rf2FullData rfData, SimulatorDataSet simData)
        {
            if ((rFactor2Constants.rF2GamePhase)rfData.scoring.mScoringInfo.mGamePhase == rFactor2Constants.rF2GamePhase.FullCourseYellow)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.FullCourseYellow;
                return;
            }

            if (rfData.scoring.mScoringInfo.mSectorFlag[0] == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
            }

            if (rfData.scoring.mScoringInfo.mSectorFlag[1] == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
            }

            if (rfData.scoring.mScoringInfo.mSectorFlag[2] == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
            }

            switch ((rFactor2Constants.rF2YellowFlagState)rfData.rules.mTrackRules.mYellowFlagState)
            {
                case rFactor2Constants.rF2YellowFlagState.Pending:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.WaitingForLeader;
                    break;
                case rFactor2Constants.rF2YellowFlagState.PitClosed:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.PitsClosed;
                    break;
                case rFactor2Constants.rF2YellowFlagState.PitLeadLap:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.PitsOpenForLeadLapCars;
                    break;
                case rFactor2Constants.rF2YellowFlagState.PitOpen:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.PitOpen;
                    break;
                case rFactor2Constants.rF2YellowFlagState.LastLap:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.LastScLap;
                    break;
                case rFactor2Constants.rF2YellowFlagState.Resume:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.LightsOff;
                    break;
                case rFactor2Constants.rF2YellowFlagState.Invalid:
                case rFactor2Constants.rF2YellowFlagState.NoFlag:
                case rFactor2Constants.rF2YellowFlagState.RaceHalt:
                default:
                    simData.SessionInfo.SafetyCarState = SafetyCarState.None;
                    break;
            }
        }

        private void AddAcceleration(SimulatorDataSet simData, rF2VehicleTelemetry playerVehicleTelemetry)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinMs = playerVehicleTelemetry.mLocalAccel.x;
            simData.PlayerInfo.CarInfo.Acceleration.YinMs = playerVehicleTelemetry.mLocalAccel.y;
            simData.PlayerInfo.CarInfo.Acceleration.ZinMs = -playerVehicleTelemetry.mLocalAccel.z;
        }

        private void AddTyresAndFuelInfo(SimulatorDataSet simData, rF2VehicleTelemetry playerVehicleTelemetry)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mPressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mPressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mPressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mPressure);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Detached = playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mDetached == 1 ||
                                                                       playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mFlat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Detached = playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mDetached == 1 ||
                                                                        playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mFlat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Detached = playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mDetached == 1 ||
                                                                      playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mFlat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Detached = playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mDetached == 1 ||
                                                                       playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mFlat == 1;

            simData.PlayerInfo.CarInfo.RearHeight = Distance.FromMeters(playerVehicleTelemetry.mRearRideHeight);
            simData.PlayerInfo.CarInfo.FrontHeight = Distance.FromMeters(playerVehicleTelemetry.mFrontWingHeight);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mCamber);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mCamber);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mCamber);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mCamber);

            //Tyre RPS
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = -playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mRotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = -playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mRotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = -playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mRotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = -playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mRotation;

            //Ride Tyre Height
            simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled = true;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mRideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mRideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mRideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mRideHeight);

            //Tyre Load
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreLoad =
                Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTireLoad);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreLoad =
                Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTireLoad);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreLoad = Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTireLoad);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreLoad =
                Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTireLoad);

            //Suspension Deflection
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mSuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mSuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mSuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel =
                Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mSuspensionDeflection);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mWear;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mWear;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mWear;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mWear;

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTireCarcassTemperature, 2000));
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = playerVehicleTelemetry.mFrontTireCompoundName.FromArray();

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTireCarcassTemperature, 2000));
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreType = playerVehicleTelemetry.mFrontTireCompoundName.FromArray();

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTireCarcassTemperature, 2000));
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreType = playerVehicleTelemetry.mRearTireCompoundName.FromArray();

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTireCarcassTemperature, 2000));
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreType = playerVehicleTelemetry.mRearTireCompoundName.FromArray();

            // Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(playerVehicleTelemetry.mFuelCapacity);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(playerVehicleTelemetry.mFuel);
        }

        private void FillPlayerCarInfo(rF2VehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            //TimeSpan playerSessionTime = TimeSpan.FromSeconds(playerVehicleTelemetry.mElapsedTime);
            //simData.SessionInfo.SessionTime = playerSessionTime;
            /*if (playerSessionTime > simData.SessionInfo.SessionTime && playerSessionTime > _lastPlayerTime)
            {
                simData.SessionInfo.SessionTime = playerSessionTime;
                _lastPlayerTime = playerSessionTime;
            }
            else
            {
                playerSessionTime = TimeSpan.Zero;
            }*/
            simData.PlayerInfo.CarInfo.EngineRpm = (int)playerVehicleTelemetry.mEngineRPM;
            simData.PlayerInfo.CarInfo.CurrentGear = playerVehicleTelemetry.mGear switch
            {
                0 => "N",
                -1 => "R",
                -2 => string.Empty,
                _ => playerVehicleTelemetry.mGear.ToString()
            };
        }

        internal void AddDriversData(SimulatorDataSet data, in Rf2FullData rfData)
        {
            if (rfData.scoring.mScoringInfo.mNumVehicles < 1)
            {
                return;
            }

            data.DriversInfo = new DriverInfo[rfData.scoring.mScoringInfo.mNumVehicles];
            DriverInfo playersInfo = null;

            for (int i = 0; i < rfData.scoring.mScoringInfo.mNumVehicles; i++)
            {
                rF2VehicleScoring rF2VehicleScoring = rfData.scoring.mVehicles[i];
                rF2VehicleTelemetry rF2VehicleTelemetry = rfData.telemetry.mVehicles[i];
                DriverInfo driverInfo = CreateDriverInfo(rfData, rF2VehicleScoring);

                if (driverInfo.IsPlayer)
                {
                    FillDrsData(driverInfo, rF2VehicleTelemetry);
                    driverInfo.CarInfo.HeadLightsStatus = rF2VehicleScoring.mHeadlights == 1 ? HeadLightsStatus.NormalBeams : HeadLightsStatus.Off;
                    playersInfo = driverInfo;
                    driverInfo.CurrentLapValid = true;
                    _lastPlayerId = rF2VehicleScoring.mID;
                }
                else
                {
                    driverInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = rF2VehicleTelemetry.mFrontTireCompoundName.FromArray();
                    driverInfo.CarInfo.WheelsInfo.FrontRight.TyreType = driverInfo.CarInfo.WheelsInfo.FrontLeft.TyreType;
                    driverInfo.CarInfo.WheelsInfo.RearRight.TyreType = rF2VehicleTelemetry.mRearTireCompoundName.FromArray();
                    driverInfo.CarInfo.WheelsInfo.RearLeft.TyreType = driverInfo.CarInfo.WheelsInfo.RearRight.TyreType;
                    driverInfo.CurrentLapValid = true;
                }

                data.DriversInfo[i] = driverInfo;
                if (driverInfo.Position == 1)
                {
                    data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                    data.LeaderInfo = driverInfo;
                }

                if (rF2VehicleScoring.mControl == 2)
                {
                    data.SessionInfo.IsMultiplayer = true;
                }

                AddLappingInformation(data, rfData, driverInfo);
                FillTimingInfo(driverInfo, rF2VehicleScoring, rfData);

                if (rfData.scoring.mScoringInfo.mSectorFlag[0] == 1 || rfData.scoring.mScoringInfo.mSectorFlag[1] == 1 || rfData.scoring.mScoringInfo.mSectorFlag[2] == 1)
                {
                    driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
                }

                if (driverInfo.FinishStatus == DriverFinishStatus.Finished && !driverInfo.IsPlayer && driverInfo.Position > _lastPlayer.Position)
                {
                    driverInfo.CompletedLaps--;
                    driverInfo.FinishStatus = DriverFinishStatus.None;
                }
            }

            CheckValidityByPlayer(playersInfo);
            if (playersInfo != null && _lastPlayer != null && playersInfo.CarName != _lastPlayer.CarName)
            {
                _playerHasHybrid = classesWithHybrid.Contains(playersInfo.CarClassId);
                _playerIsElectricOnly = classesWithElectricOnly.Contains(playersInfo.CarClassId);
            }

            _lastPlayer = playersInfo;
            if (playersInfo != null)
            {
                data.PlayerInfo = playersInfo;
            }

            FillGapInformation(data.DriversInfo);
        }

        private void FillDrsData(DriverInfo player, rF2VehicleTelemetry rF2VehicleTelemetry)
        {
            DrsSystem drsSystem = player.CarInfo.DrsSystem;
            if (rF2VehicleTelemetry.mRearFlapActivated == 1)
            {
                drsSystem.DrsStatus = DrsStatus.InUse;
                return;
            }

            switch ((rFactor2Constants.rF2RearFlapLegalStatus)rF2VehicleTelemetry.mRearFlapLegalStatus)
            {
                case rFactor2Constants.rF2RearFlapLegalStatus.DetectedButNotAllowedYet:
                    drsSystem.DrsStatus = DrsStatus.Equipped;
                    break;
                case rFactor2Constants.rF2RearFlapLegalStatus.Alllowed:
                    drsSystem.DrsStatus = DrsStatus.Available;
                    break;
                case rFactor2Constants.rF2RearFlapLegalStatus.Disallowed:
                default:
                    drsSystem.DrsStatus = DrsStatus.NotEquipped;
                    break;
            }
        }

        private void FillGapInformation(DriverInfo[] drivers)
        {
            DriverInfo[] orderedDrivers = drivers.OrderBy(x => x.Position).ToArray();

            for (int i = 1; i < orderedDrivers.Length; i++)
            {
                orderedDrivers[i - 1].Timing.GapBehind = orderedDrivers[i].Timing.GapAhead;
            }
        }

        private void CheckValidityByPlayer(DriverInfo driver)
        {
            if (_lastPlayer == null || driver == null || (!_lastPlayer.InPits && driver.InPits))
            {
                return;
            }

            Distance distance = Point3D.GetDistance(driver.WorldPosition, _lastPlayer.WorldPosition);
            if (distance.InMeters > 200)
            {
                _currentlyIgnoredPackage++;
                if (_currentlyIgnoredPackage < MaxConsecutivePackagesIgnored)
                {
                    throw new RF2InvalidPackageException("Players distance was :" + distance.InMeters);
                }
            }
        }

        internal void FillTimingInfo(DriverInfo driverInfo, rF2VehicleScoring rfVehicleInfo, in Rf2FullData rf2FullData)
        {
            driverInfo.Timing.GapAhead = TimeSpan.FromSeconds(rfVehicleInfo.mTimeBehindNext);
            driverInfo.Timing.LastSector1Time = CreateTimeSpan(rfVehicleInfo.mCurSector1);
            driverInfo.Timing.LastSector2Time = CreateTimeSpan(rfVehicleInfo.mCurSector2 - rfVehicleInfo.mCurSector1);
            driverInfo.Timing.LastSector3Time = CreateTimeSpan(rfVehicleInfo.mLastLapTime - rfVehicleInfo.mLastSector2);
            driverInfo.Timing.LastLapTime = CreateTimeSpan(rfVehicleInfo.mLastLapTime);
            driverInfo.Timing.CurrentSector = rfVehicleInfo.mSector == 0 ? 3 : rfVehicleInfo.mSector;
            driverInfo.Timing.CurrentLapTime = CreateTimeSpan(rf2FullData.scoring.mScoringInfo.mCurrentET - rfVehicleInfo.mLapStartET);
        }

        private TimeSpan CreateTimeSpan(double seconds)
        {
            return seconds > 0 ? _sessionTimeInterpolator.ApplyInterpolation(TimeSpan.FromSeconds(seconds)) : TimeSpan.Zero;
        }

        private void AddLappingInformation(SimulatorDataSet data, in Rf2FullData rfData, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType == SessionType.Race && _lastPlayer != null
                                                                 && _lastPlayer.CompletedLaps != 0)
            {
                driverInfo.IsBeingLappedByPlayer =
                    driverInfo.TotalDistance < (_lastPlayer.TotalDistance - (rfData.scoring.mScoringInfo.mLapDist * 0.5));
                driverInfo.IsLappingPlayer =
                    _lastPlayer.TotalDistance < (driverInfo.TotalDistance - (rfData.scoring.mScoringInfo.mLapDist * 0.5));
            }
        }

        private DriverInfo CreateDriverInfo(in Rf2FullData rfData, rF2VehicleScoring rfVehicleInfo)
        {
            DriverInfo driverInfo = new()
            {
                DriverSessionId = rfVehicleInfo.mDriverName.FromArray(),
                CompletedLaps = rfVehicleInfo.mTotalLaps,
                CarName = rfVehicleInfo.mVehicleName.FromArray(),
                CarClassName = rfVehicleInfo.mVehicleClass.FromArray(),
                InPits = rfVehicleInfo.mInPits == 1,
                PitStopRequested = rfVehicleInfo.mPitState == 1,
            };
            driverInfo.DriverShortName = driverInfo.DriverLongName = driverInfo.DriverSessionId;
            driverInfo.CarClassId = driverInfo.CarClassName;
            driverInfo.IsPlayer = rfVehicleInfo.mIsPlayer == 1;
            driverInfo.Position = rfVehicleInfo.mPlace;
            driverInfo.Speed = Velocity.FromMs(Math.Sqrt((rfVehicleInfo.mLocalVel.x * rfVehicleInfo.mLocalVel.x)
                                                         + (rfVehicleInfo.mLocalVel.y * rfVehicleInfo.mLocalVel.y)
                                                         + (rfVehicleInfo.mLocalVel.z * rfVehicleInfo.mLocalVel.z)));
            driverInfo.LapDistance = rfVehicleInfo.mLapDist;
            driverInfo.TotalDistance = (rfVehicleInfo.mTotalLaps * rfData.scoring.mScoringInfo.mLapDist) + rfVehicleInfo.mLapDist;
            driverInfo.FinishStatus = FromRfStatus(rfVehicleInfo.mFinishStatus);
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(-rfVehicleInfo.mPos.x), Distance.FromMeters(rfVehicleInfo.mPos.y),
                Distance.FromMeters(rfVehicleInfo.mPos.z));
            ComputeDistanceToPlayer(_lastPlayer, driverInfo, rfData);
            return driverInfo;
        }

        internal static void ComputeDistanceToPlayer(DriverInfo player, DriverInfo driverInfo, in Rf2FullData rf2FullData)
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

            double trackLength = rf2FullData.scoring.mScoringInfo.mLapDist;
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

        internal void FillSessionInfo(in Rf2FullData data, SimulatorDataSet simData)
        {
            // Timing
            simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(data.scoring.mScoringInfo.mLapDist);
            simData.SessionInfo.TrackInfo.TrackName = data.scoring.mScoringInfo.mTrackName.FromArray();
            simData.SessionInfo.TrackInfo.TrackLayoutName = string.Empty;
            simData.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(data.scoring.mScoringInfo.mAmbientTemp);
            simData.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(data.scoring.mScoringInfo.mTrackTemp);
            simData.SessionInfo.WeatherInfo.RainIntensity = (int)(data.scoring.mScoringInfo.mRaining * 100);
            simData.SessionInfo.WeatherInfo.TrackWetness = (int)(((data.scoring.mScoringInfo.mMinPathWetness + data.scoring.mScoringInfo.mMaxPathWetness) / 2.0) * 100);
            simData.SessionInfo.SessionTime = TimeSpan.FromSeconds(data.scoring.mScoringInfo.mCurrentET);

            //_sessionTimeInterpolator.Visit(simData);

            if (data.extended.mTicksSessionEnded > data.extended.mTicksSessionStarted)
            {
                simData.SessionInfo.SessionType = SessionType.Na;
                simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                return;
            }

            if (data.scoring.mScoringInfo is { mTrackTemp: 0, mSession: 0, mGamePhase: 0 }
                && string.IsNullOrEmpty(simData.SessionInfo.TrackInfo.TrackName)
                && data.scoring.mScoringInfo.mLapDist == 0)
            {
                simData.SessionInfo.SessionType = SessionType.Na;
                simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                return;
            }

            switch (data.scoring.mScoringInfo.mSession)
            {
                case -1:
                    simData.SessionInfo.SessionType = SessionType.Na;
                    break;
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                    simData.SessionInfo.SessionType = SessionType.Qualification;
                    break;
                case 9:
                    simData.SessionInfo.SessionType = SessionType.WarmUp;
                    break;
                case 10:
                case 11:
                case 12:
                case 13:
                    simData.SessionInfo.SessionType = SessionType.Race;
                    break;
                default:
                    simData.SessionInfo.SessionType = SessionType.Practice;
                    break;
            }

            switch ((rFactor2Constants.rF2GamePhase)data.scoring.mScoringInfo.mGamePhase)
            {
                case rFactor2Constants.rF2GamePhase.Garage:
                    break;
                case rFactor2Constants.rF2GamePhase.WarmUp:
                case rFactor2Constants.rF2GamePhase.GridWalk:
                case rFactor2Constants.rF2GamePhase.Formation:
                case rFactor2Constants.rF2GamePhase.Countdown:
                    simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                    break;
                case rFactor2Constants.rF2GamePhase.SessionStopped:
                case rFactor2Constants.rF2GamePhase.FullCourseYellow:
                case rFactor2Constants.rF2GamePhase.GreenFlag:
                    simData.SessionInfo.SessionPhase = SessionPhase.Green;
                    break;
                case rFactor2Constants.rF2GamePhase.SessionOver:
                    simData.SessionInfo.SessionPhase = SessionPhase.Checkered;
                    break;
            }

            simData.SessionInfo.IsActive = simData.SessionInfo.SessionType != SessionType.Na;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (data.scoring.mScoringInfo.mEndET > 0)
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Time;
                simData.SessionInfo.SessionTimeRemaining =
                    data.scoring.mScoringInfo.mEndET - data.scoring.mScoringInfo.mCurrentET > 0 ? data.scoring.mScoringInfo.mEndET - data.scoring.mScoringInfo.mCurrentET : 0;
            }
            else
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Laps;
                simData.SessionInfo.TotalNumberOfLaps = data.scoring.mScoringInfo.mMaxLaps;
            }
        }
    }
}

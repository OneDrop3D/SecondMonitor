namespace SecondMonitor.LmUConnector.SharedMemory
{
    using System;
    using System.Linq;

    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.Foundation.Connectors;
    using SecondMonitor.Foundation.Connectors.Visitor;
    using SecondMonitor.LmUConnector.Content;
    using SecondMonitor.LmUConnector.SharedMemory.lmuData;

    internal class LmUDataConverter : AbstractDataConvertor
    {
        //private const int MaxConsecutivePackagesIgnored = 200;
        private readonly SessionTimeInterpolator _sessionTimeInterpolator;
        private readonly LmUContent _lmUContent;

        private DriverInfo _lastPlayer = new();
        private LmUVehicleTelemetry _lastPlayerTelemetry;

        //private int _currentlyIgnoredPackage;
        //private TimeSpan _lastPlayerTime = TimeSpan.Zero;

        public LmUDataConverter(SessionTimeInterpolator sessionTimeInterpolator)
        {
            _sessionTimeInterpolator = sessionTimeInterpolator;
            _lmUContent = new LmUContent();
        }

        public SimulatorDataSet CreateSimulatorDataSet(in LmUObjectOut lmData)
        {
            try
            {
                SimulatorDataSet simData = new(SimulatorsNameMap.LmUSimName)
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

                FillSessionInfo(lmData, simData);
                AddFlags(lmData, simData);
                AddDriversData(simData, lmData);

                if(lmData.Telemetry.playerHasVehicle == 0)
                {
                    return simData;
                }

                LmUVehicleTelemetry playelmUVehicleTelemetry = lmData.Telemetry.mVehicles[lmData.Telemetry.playerVehicleIdx];

                //This is unreliable, it resets at long races
                /*if (playelmUVehicleTelemetry.mElapsedTime > 0)
                {
                    simData.SessionInfo.SessionTime = TimeSpan.FromSeconds(playelmUVehicleTelemetry.mElapsedTime);
                }*/

                if (playelmUVehicleTelemetry.mWheels == null && _lastPlayerTelemetry.mWheels != null)
                {
                    playelmUVehicleTelemetry = _lastPlayerTelemetry;
                }
                else
                {
                    _lastPlayerTelemetry = playelmUVehicleTelemetry;
                }

                if (playelmUVehicleTelemetry.mWheels == null)
                {
                    return simData;
                }

                FillPlayerCarInfo(playelmUVehicleTelemetry, simData);

                // PEDAL INFO
                AddPedalInfo(playelmUVehicleTelemetry, simData);

                // WaterSystemInfo
                AddWaterSystemInfo(playelmUVehicleTelemetry, simData);

                // OilSystemInfo
                AddOilSystemInfo(playelmUVehicleTelemetry, simData);

                // Brakes Info
                AddBrakesInfo(playelmUVehicleTelemetry, simData);

                // Tyre Pressure Info
                AddTyresAndFuelInfo(simData, playelmUVehicleTelemetry);

                // Acceleration
                AddAcceleration(simData, playelmUVehicleTelemetry);

                //Add Additional Player Car Info
                AddPlayerCarInfo(playelmUVehicleTelemetry, simData);

                /*_currentlyIgnoredPackage = 0;*/

                PopulateClassPositions(simData);

                FillSpectateInfo(simData, lmData);

                return simData;
            }
            catch (Exception ex)
            {
                _lastPlayer = new DriverInfo();
                throw new LMUInvalidPackageException(ex);
            }
        }

        private void FillSpectateInfo(SimulatorDataSet data, in LmUObjectOut lmData)
        {
            if (lmData.Telemetry.playerHasVehicle == 0)
            {
                data.SessionInfo.SpectatingState = SpectatingState.Spectate;
            }

            data.SessionInfo.SpectatingState = lmData.Scoring.mVehicles[lmData.Telemetry.playerVehicleIdx].mControl != 3 ? SpectatingState.Live : SpectatingState.Spectate;
        }

        internal static DriverFinishStatus FromRfStatus(int finishStatus)
        {
            return (LmuConstants.LmUFinishStatus)finishStatus switch
            {
                LmuConstants.LmUFinishStatus.None => DriverFinishStatus.None,
                LmuConstants.LmUFinishStatus.Dnf => DriverFinishStatus.Dnf,
                LmuConstants.LmUFinishStatus.Dq => DriverFinishStatus.Dq,
                LmuConstants.LmUFinishStatus.Finished => DriverFinishStatus.Finished,
                _ => DriverFinishStatus.Na
            };
        }

        private static void AddBrakesInfo(LmUVehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mBrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mBrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mBrakeTemp);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity = Temperature.FromKelvin(
                playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mBrakeTemp);
        }

        private static void AddOilSystemInfo(LmUVehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(playerVehicleTelemetry.mEngineOilTemp);
            simData.PlayerInfo.CarInfo.TurboPressure = Pressure.FromKiloPascals(playerVehicleTelemetry.mTurboBoostPressure / 1000);
        }

        private static void AddWaterSystemInfo(LmUVehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(playerVehicleTelemetry.mEngineWaterTemp);
        }

        private static void AddPedalInfo(LmUVehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = playerVehicleTelemetry.mUnfilteredThrottle;
            simData.InputInfo.BrakePedalPosition = playerVehicleTelemetry.mUnfilteredBrake;
            simData.InputInfo.ClutchPedalPosition = playerVehicleTelemetry.mUnfilteredClutch;
            simData.InputInfo.SteeringInput = playerVehicleTelemetry.mUnfilteredSteering;
        }

        private void AddPlayerCarInfo(LmUVehicleTelemetry data, SimulatorDataSet simData)
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

            if (data.mABSMax > 0)
            {
                playerCar.AbsInfo.IsAvailable = true;
                playerCar.AbsInfo.IsActive = data.mABSActive == 1;
                playerCar.AbsInfo.Remark = data.mABS.ToString();
            }

            if (data.mTCMax > 0 || data.mTCSlipMax > 0 || data.mTCCutMax > 0)
            {
                playerCar.TcInfo.IsAvailable = true;
                playerCar.TcInfo.IsActive = data.mTCActive == 1;
                string tcInfo = data.mTCMax > 0 ? data.mTC.ToString() : string.Empty;
                string tcCutInfo = data.mTCCutMax > 0 ? data.mTCCut.ToString() : string.Empty;
                string tcSlipInfo = data.mTCSlipMax > 0 ? data.mTCSlip.ToString() : string.Empty;
                playerCar.TcInfo.Remark = string.Join("/", tcInfo, tcCutInfo, tcSlipInfo).Trim();
            }

            playerCar.SpeedLimiterEngaged = data.mSpeedLimiter == 1;
            playerCar.FrontDownForce = Force.GetFromNewtons(data.mFrontDownforce);
            playerCar.RearDownForce = Force.GetFromNewtons(data.mRearDownforce);
            playerCar.OverallDownForce = Force.GetFromNewtons(data.mFrontDownforce + data.mRearDownforce);
        }

        private void AddFlags(in LmUObjectOut lmData, SimulatorDataSet simData)
        {
            if ((LmuConstants.LmUGamePhase)lmData.Scoring.mScoringInfo.mGamePhase == LmuConstants.LmUGamePhase.FullCourseYellow)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.FullCourseYellow;
                return;
            }

            if (lmData.Scoring.mScoringInfo.mSectorFlag[0] == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
            }

            if (lmData.Scoring.mScoringInfo.mSectorFlag[1] == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
            }

            if (lmData.Scoring.mScoringInfo.mSectorFlag[2] == 1)
            {
                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
            }
        }

        private void AddAcceleration(SimulatorDataSet simData, LmUVehicleTelemetry playerVehicleTelemetry)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinMs = playerVehicleTelemetry.mLocalAccel.x;
            simData.PlayerInfo.CarInfo.Acceleration.YinMs = playerVehicleTelemetry.mLocalAccel.y;
            simData.PlayerInfo.CarInfo.Acceleration.ZinMs = -playerVehicleTelemetry.mLocalAccel.z;
        }

        private void AddTyresAndFuelInfo(SimulatorDataSet simData, LmUVehicleTelemetry playerVehicleTelemetry)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mPressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mPressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mPressure);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity =
                Pressure.FromKiloPascals(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mPressure);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Detached = playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mDetached == 1 || playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mFlat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Detached = playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mDetached == 1 || playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mFlat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Detached = playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mDetached == 1 || playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mFlat == 1;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Detached = playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mDetached == 1 || playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mFlat == 1;

            simData.PlayerInfo.CarInfo.RearHeight = Distance.FromMeters(playerVehicleTelemetry.mRearRideHeight);
            simData.PlayerInfo.CarInfo.FrontHeight = Distance.FromMeters(playerVehicleTelemetry.mFrontWingHeight);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mCamber);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mCamber);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mCamber);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Camber = Angle.GetFromRadians(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mCamber);

            //Tyre RPS
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = -playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mRotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = -playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mRotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = -playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mRotation;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = -playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mRotation;

            //Ride Tyre Height
            simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled = true;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mRideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mRideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mRideHeight);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mRideHeight);

            //Tyre Load
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreLoad = Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mTireLoad);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreLoad = Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mTireLoad);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreLoad = Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mTireLoad);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreLoad = Force.GetFromNewtons(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mTireLoad);

            //Suspension Deflection
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mSuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mSuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mSuspensionDeflection);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromMeters(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mSuspensionDeflection);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mWear;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mWear;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mWear;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear =
                1 - playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mWear;

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontLeft].mTireCarcassTemperature, 2000));

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.FrontRight].mTireCarcassTemperature, 2000));

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearLeft].mTireCarcassTemperature, 2000));

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mTemperature[0]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mTemperature[2]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.ActualQuantity =
                Temperature.FromKelvin(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mTemperature[1]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity =
                Temperature.FromKelvin(Math.Min(playerVehicleTelemetry.mWheels[(int)LmuConstants.LmUWheelIndex.RearRight].mTireCarcassTemperature, 2000));

            // Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(playerVehicleTelemetry.mFuelCapacity);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(playerVehicleTelemetry.mFuel);
        }

        private void FillPlayerCarInfo(LmUVehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
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
            FillBoostInfo(playerVehicleTelemetry, simData);
            simData.PlayerInfo.CarInfo.EngineRpm = (int)playerVehicleTelemetry.mEngineRPM;
            simData.PlayerInfo.CarInfo.CurrentGear = playerVehicleTelemetry.mGear switch
            {
                0 => "N",
                -1 => "R",
                -2 => string.Empty,
                _ => playerVehicleTelemetry.mGear.ToString()
            };
        }

        private void FillBoostInfo(LmUVehicleTelemetry playerVehicleTelemetry, SimulatorDataSet simData)
        {
            if (playerVehicleTelemetry.mElectricBoostMotorState == 0)
            {
                return;
            }

            HybridSystem hybridSystem = simData.PlayerInfo.CarInfo.HybridSystem;
            hybridSystem.RemainingChargePercentage = playerVehicleTelemetry.mBatteryChargeFraction;
            hybridSystem.HybridSystemMode = HybridSystemMode.Balanced;

            switch (playerVehicleTelemetry.mElectricBoostMotorState)
            {
                case 1:
                    hybridSystem.HybridChargeChange = HybridChargeChange.None;
                    return;
                case 2:
                    hybridSystem.HybridChargeChange = HybridChargeChange.Discharge;
                    return;
                case 3:
                    hybridSystem.HybridChargeChange = HybridChargeChange.Recharge;
                    return;
                default:
                    return;
            }
        }

        internal void AddDriversData(SimulatorDataSet data, in LmUObjectOut lmData)
        {
            if (lmData.Scoring.mScoringInfo.mNumVehicles < 1)
            {
                return;
            }

            data.DriversInfo = new DriverInfo[lmData.Scoring.mScoringInfo.mNumVehicles];
            DriverInfo playersInfo = null;

            for (int i = 0; i < lmData.Scoring.mScoringInfo.mNumVehicles; i++)
            {
                LmUVehicleScoring lmUVehicleScoring = lmData.Scoring.mVehicles[i];
                LmUVehicleTelemetry lmUVehicleTelemetry = lmData.Telemetry.mVehicles[i];
                DriverInfo driverInfo = CreateDriverInfo(lmData, lmUVehicleScoring, lmUVehicleTelemetry);

                if (driverInfo.IsPlayer)
                {
                    driverInfo.CarInfo.HeadLightsStatus = lmUVehicleScoring.mHeadlights == 1 ? HeadLightsStatus.NormalBeams : HeadLightsStatus.Off;
                    playersInfo = driverInfo;
                    driverInfo.CurrentLapValid = true;
                }
                else
                {
                    driverInfo.CurrentLapValid = true;
                }

                FillVirtualEnergyAndTyres(driverInfo, lmUVehicleTelemetry);

                data.DriversInfo[i] = driverInfo;
                if (driverInfo.Position == 1)
                {
                    data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                    data.LeaderInfo = driverInfo;
                }

                if (lmUVehicleScoring.mControl == 2)
                {
                    data.SessionInfo.IsMultiplayer = true;
                }

                AddLappingInformation(data, lmData, driverInfo);
                FillTimingInfo(driverInfo, lmUVehicleScoring, lmData);

                if (lmData.Scoring.mScoringInfo.mSectorFlag[0] == 1 || lmData.Scoring.mScoringInfo.mSectorFlag[1] == 1 || lmData.Scoring.mScoringInfo.mSectorFlag[2] == 1)
                {
                    driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
                }

                if (driverInfo.FinishStatus == DriverFinishStatus.Finished && !driverInfo.IsPlayer && driverInfo.Position > _lastPlayer.Position)
                {
                    driverInfo.CompletedLaps--;
                    driverInfo.FinishStatus = DriverFinishStatus.None;
                }
            }

            //CheckValidityByPlayer(playersInfo);
            _lastPlayer = playersInfo;
            if (playersInfo != null)
            {
                data.PlayerInfo = playersInfo;
            }

            FillGapInformation(data.DriversInfo);
        }

        private void FillGapInformation(DriverInfo[] drivers)
        {
            DriverInfo[] orderedDrivers = drivers.OrderBy(x => x.Position).ToArray();

            for (int i = 1; i < orderedDrivers.Length; i++)
            {
                orderedDrivers[i - 1].Timing.GapBehind = orderedDrivers[i].Timing.GapAhead;
            }
        }

        /*private void CheckValidityByPlayer(DriverInfo driver)
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
                    throw new LMUInvalidPackageException("Players distance was :" + distance.InMeters);
                }
            }
        }*/

        private void FillVirtualEnergyAndTyres(DriverInfo driverInfo, LmUVehicleTelemetry lmUVehicleTelemetry)
        {
            if (((LmuConstants.LmuVehicleClass)lmUVehicleTelemetry.mVehicleClass) is LmuConstants.LmuVehicleClass.Hypercar or LmuConstants.LmuVehicleClass.Gt3)
            {
                driverInfo.CarInfo.FuelSystemInfo.HasVirtualTank = true;
                driverInfo.CarInfo.FuelSystemInfo.VirtualTank = Percentage.FromRatio(lmUVehicleTelemetry.mVirtualEnergy);
            }

            driverInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = lmUVehicleTelemetry.mFrontTireCompoundName.FromArray();
            driverInfo.CarInfo.WheelsInfo.FrontRight.TyreType = driverInfo.CarInfo.WheelsInfo.FrontLeft.TyreType;
            driverInfo.CarInfo.WheelsInfo.RearRight.TyreType = lmUVehicleTelemetry.mRearTireCompoundName.FromArray();
            driverInfo.CarInfo.WheelsInfo.RearLeft.TyreType = driverInfo.CarInfo.WheelsInfo.RearRight.TyreType;
        }

        internal void FillTimingInfo(DriverInfo driverInfo, LmUVehicleScoring rfVehicleInfo, in LmUObjectOut lmUObjectOut)
        {
            driverInfo.Timing.GapAhead = TimeSpan.FromSeconds(rfVehicleInfo.mTimeBehindNext);
            driverInfo.Timing.LastSector1Time = CreateTimeSpan(rfVehicleInfo.mCurSector1);
            driverInfo.Timing.LastSector2Time = CreateTimeSpan(rfVehicleInfo.mCurSector2 - rfVehicleInfo.mCurSector1);
            driverInfo.Timing.LastSector3Time = CreateTimeSpan(rfVehicleInfo.mLastLapTime - rfVehicleInfo.mLastSector2);
            driverInfo.Timing.LastLapTime = CreateTimeSpan(rfVehicleInfo.mLastLapTime);
            driverInfo.Timing.CurrentSector = rfVehicleInfo.mSector == 0 ? 3 : rfVehicleInfo.mSector;
            driverInfo.Timing.CurrentLapTime = CreateTimeSpan(lmUObjectOut.Scoring.mScoringInfo.mCurrentET - rfVehicleInfo.mLapStartET);
        }

        private TimeSpan CreateTimeSpan(double seconds)
        {
            return seconds > 0 ? _sessionTimeInterpolator.ApplyInterpolation(TimeSpan.FromSeconds(seconds)) : TimeSpan.Zero;
        }

        private void AddLappingInformation(SimulatorDataSet data, in LmUObjectOut lmData, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType == SessionType.Race && _lastPlayer != null
                                                                 && _lastPlayer.CompletedLaps != 0)
            {
                driverInfo.IsBeingLappedByPlayer =
                    driverInfo.TotalDistance < (_lastPlayer.TotalDistance - (lmData.Scoring.mScoringInfo.mLapDist * 0.5));
                driverInfo.IsLappingPlayer =
                    _lastPlayer.TotalDistance < (driverInfo.TotalDistance - (lmData.Scoring.mScoringInfo.mLapDist * 0.5));
            }
        }

        private DriverInfo CreateDriverInfo(in LmUObjectOut lmData, LmUVehicleScoring rfVehicleInfo, LmUVehicleTelemetry lmUVehicleTelemetry)
        {
            string lmuCarName = rfVehicleInfo.mVehicleName.FromArray();
            LmUCar lmUCar = _lmUContent.GetCarSafe(lmuCarName);
            DriverInfo driverInfo = new()
            {
                DriverSessionId = rfVehicleInfo.mID.ToString(),
                CompletedLaps = rfVehicleInfo.mTotalLaps,
                CarName = lmUVehicleTelemetry.mVehicleModel.FromArray(),
                TeamName = lmUCar.TeamName,
                CarRaceNumber = lmUCar.Number,
                IsCarRaceNumberFilled = true,
                CarClassName = rfVehicleInfo.mVehicleClass.FromArray(),
                InPits = rfVehicleInfo.mInPits == 1,
                PitStopRequested = rfVehicleInfo.mPitState == 1,
            };

            driverInfo.DriverShortName = driverInfo.DriverLongName = rfVehicleInfo.mDriverName.FromArray();
            driverInfo.CarClassId = driverInfo.CarClassName;
            driverInfo.IsPlayer = rfVehicleInfo.mIsPlayer == 1;
            driverInfo.Position = rfVehicleInfo.mPlace;
            driverInfo.Speed = Velocity.FromMs(Math.Sqrt((rfVehicleInfo.mLocalVel.x * rfVehicleInfo.mLocalVel.x)
                                                         + (rfVehicleInfo.mLocalVel.y * rfVehicleInfo.mLocalVel.y)
                                                         + (rfVehicleInfo.mLocalVel.z * rfVehicleInfo.mLocalVel.z)));
            driverInfo.LapDistance = rfVehicleInfo.mLapDist;
            driverInfo.TotalDistance = (rfVehicleInfo.mTotalLaps * lmData.Scoring.mScoringInfo.mLapDist) + rfVehicleInfo.mLapDist;
            driverInfo.FinishStatus = FromRfStatus(rfVehicleInfo.mFinishStatus);
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(-rfVehicleInfo.mPos.x), Distance.FromMeters(rfVehicleInfo.mPos.y), Distance.FromMeters(rfVehicleInfo.mPos.z));
            ComputeDistanceToPlayer(_lastPlayer, driverInfo, lmData);
            return driverInfo;
        }

        internal static void ComputeDistanceToPlayer(DriverInfo player, DriverInfo driverInfo, in LmUObjectOut lmUObjectOut)
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

            double trackLength = lmUObjectOut.Scoring.mScoringInfo.mLapDist;
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

        internal void FillSessionInfo(in LmUObjectOut data, SimulatorDataSet simData)
        {
            // Timing
            simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(data.Scoring.mScoringInfo.mLapDist);
            simData.SessionInfo.TrackInfo.TrackName = data.Scoring.mScoringInfo.mTrackName.FromArray();
            simData.SessionInfo.TrackInfo.TrackLayoutName = string.Empty;
            simData.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(data.Scoring.mScoringInfo.mAmbientTemp);
            simData.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(data.Scoring.mScoringInfo.mTrackTemp);
            simData.SessionInfo.WeatherInfo.RainIntensity = (int)(data.Scoring.mScoringInfo.mRaining * 100);
            simData.SessionInfo.WeatherInfo.TrackWetness = (int)(((data.Scoring.mScoringInfo.mMinPathWetness + data.Scoring.mScoringInfo.mMaxPathWetness) / 2.0) * 100);
            simData.SessionInfo.SessionTime = TimeSpan.FromSeconds(data.Scoring.mScoringInfo.mCurrentET);

            //_sessionTimeInterpolator.Visit(simData);

            if (data.Scoring.mScoringInfo is { mTrackTemp: 0, mSession: 0, mGamePhase: 0 }
                && string.IsNullOrEmpty(simData.SessionInfo.TrackInfo.TrackName)
                && data.Scoring.mScoringInfo.mLapDist == 0)
            {
                simData.SessionInfo.SessionType = SessionType.Na;
                simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                return;
            }

            simData.SessionInfo.SessionType = data.Scoring.mScoringInfo.mSession switch
            {
                -1 => SessionType.Na,
                0 or 1 or 2 or 3 or 4 => SessionType.Practice,
                5 or 6 or 7 or 8 => SessionType.Qualification,
                9 => SessionType.WarmUp,
                10 or 11 or 12 or 13 => SessionType.Race,
                _ => SessionType.Practice
            };

            switch ((LmuConstants.LmUGamePhase)data.Scoring.mScoringInfo.mGamePhase)
            {
                case LmuConstants.LmUGamePhase.Garage:
                    break;
                case LmuConstants.LmUGamePhase.WarmUp:
                case LmuConstants.LmUGamePhase.GridWalk:
                case LmuConstants.LmUGamePhase.Formation:
                case LmuConstants.LmUGamePhase.Countdown:
                    simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
                    break;
                case LmuConstants.LmUGamePhase.SessionStopped:
                case LmuConstants.LmUGamePhase.FullCourseYellow:
                case LmuConstants.LmUGamePhase.GreenFlag:
                    simData.SessionInfo.SessionPhase = SessionPhase.Green;
                    break;
                case LmuConstants.LmUGamePhase.SessionOver:
                    simData.SessionInfo.SessionPhase = SessionPhase.Checkered;
                    break;
            }

            simData.SessionInfo.IsActive = simData.SessionInfo.SessionType != SessionType.Na;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (data.Scoring.mScoringInfo.mEndET > 0)
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Time;
                simData.SessionInfo.SessionTimeRemaining =
                    data.Scoring.mScoringInfo.mEndET - data.Scoring.mScoringInfo.mCurrentET > 0 ? data.Scoring.mScoringInfo.mEndET - data.Scoring.mScoringInfo.mCurrentET : 0;
            }
            else
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Laps;
                simData.SessionInfo.TotalNumberOfLaps = data.Scoring.mScoringInfo.mMaxLaps;
            }
        }
    }
}
namespace SecondMonitor.AMS2Connector.DataConvertor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using Foundation.Connectors;
    using PluginsConfiguration.Common.DataModel;

    using SecondMonitor.DataModel.Extensions;

    using SharedMemory;

    public class Ams2DataConvertor : AbstractDataConvertor
    {
        private readonly Ams2Configuration _ams2Configuration;
        private readonly Dictionary<string, string> _customClassMap;
        private DriverInfo _lastPlayer = new DriverInfo();
        private TimeSpan _lastSwitchedToRacingTime;
        private string _lastPlayerShortName;
        private RaceState _lastRaceState;

        public Ams2DataConvertor(Ams2Configuration ams2Configuration)
        {
            _customClassMap = new Dictionary<string, string>();
            _ams2Configuration = ams2Configuration;
            _lastPlayerShortName = string.Empty;
            InitializeCustomClassMap();
        }

        public TimeSpan LastSessionTime { get; set; }

        public SimulatorDataSet CreateSimulatorDataSet(ref Ams2SharedMemory ams2data, TimeSpan sessionTime)
        {
            SimulatorDataSet simData = new SimulatorDataSet(SimulatorsNameMap.Ams2SimName)
            {
                SimulatorSourceInfo =
                {
                    HasLapTimeInformation = true,
                    OutLapIsValid = true,
                    InvalidateLapBySector = true,
                    SectorTimingSupport = DataInputSupport.Full,
                    TelemetryInfo = { ContainsSuspensionTravel = true, ContainsSuspensionVelocity = true, ContainsWheelRps = true, IsSuspensionTravelInverted = true }
                }
            };

            FillSessionInfo(ref ams2data, simData, sessionTime);
            AddActiveFlags(ref ams2data, simData);
            AddDriversData(simData, ref ams2data);

            FillPlayerCarInfo(ref ams2data, simData);

            // PEDAL INFO
            AddPedalInfo(ref ams2data, simData);

            // WaterSystemInfo
            AddWaterSystemInfo(ref ams2data, simData);

            // OilSystemInfo
            AddOilSystemInfo(ref ams2data, simData);

            // Brakes Info
            AddBrakesInfo(ref ams2data, simData);

            // Tyre Pressure Info
            AddTyresAndFuelInfo(ref ams2data, simData);

            //Add Additional Player Car Info
            AddPlayerCarInfo(ref ams2data, simData);

            // Acceleration
            AddAcceleration(ref ams2data, simData);

            if (simData.PlayerInfo?.FinishStatus == DriverFinishStatus.Dns && simData.SessionInfo.SessionType == SessionType.Race)
            {
                simData.SessionInfo.SessionPhase = SessionPhase.Countdown;
            }

            PopulateClassPositions(simData);

            FillSpectatingInfo(ref ams2data, simData);
            FillPitStopWindow(ref ams2data, simData);

            RaceState currentRaceState = (RaceState)ams2data.mRaceState;
            if (_lastRaceState != RaceState.RaceStateRacing && currentRaceState == RaceState.RaceStateRacing)
            {
                _lastSwitchedToRacingTime = LastSessionTime;
            }

            _lastRaceState = currentRaceState;
            return simData;
        }

        private static void AddBrakesInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.mBrakeTempCelsius[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.mBrakeTempCelsius[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.mBrakeTempCelsius[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(data.mBrakeTempCelsius[(int)WheelIndex.TyreRearRight]);
        }

        private static void AddOilSystemInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(data.mOilTempCelsius);
            simData.PlayerInfo.CarInfo.OilSystemInfo.OilPressure = Pressure.FromKiloPascals(data.mOilPressureKPa);
        }

        private static void AddWaterSystemInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(data.mWaterTempCelsius);
            simData.PlayerInfo.CarInfo.WaterSystemInfo.WaterPressure = Pressure.FromKiloPascals(data.mWaterPressureKPa);
        }

        private static void AddPedalInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.InputInfo.ThrottlePedalPosition = data.mUnfilteredThrottle;
            simData.InputInfo.BrakePedalPosition = data.mUnfilteredBrake;
            simData.InputInfo.ClutchPedalPosition = data.mUnfilteredClutch;
            simData.InputInfo.SteeringInput = data.mUnfilteredSteering;
        }

        private static void FillPlayerCarInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.EngineRpm = (int)data.mRpm;
            switch (data.mGear)
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
                    simData.PlayerInfo.CarInfo.CurrentGear = data.mGear.ToString();
                    break;
            }
        }

        private static TimeSpan CreateTimeSpan(double seconds)
        {
            return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        }

        private static DriverFinishStatus FromPCarStatus(RaceState finishStatus)
        {
            switch (finishStatus)
            {
                case RaceState.RaceStateInvalid:
                    return DriverFinishStatus.Na;

                case RaceState.RaceStateNotStarted:
                    return DriverFinishStatus.Dns;

                case RaceState.RaceStateRacing:
                    return DriverFinishStatus.None;

                case RaceState.RaceStateFinished:
                    return DriverFinishStatus.Finished;

                case RaceState.RaceStateDisqualified:
                    return DriverFinishStatus.Dnq;

                case RaceState.RaceStateRetired:
                    return DriverFinishStatus.Dnf;

                case RaceState.RaceStateDnf:
                    return DriverFinishStatus.Dnf;
                case RaceState.RaceStateMax:
                    return DriverFinishStatus.Na;
                default:
                    return DriverFinishStatus.Na;
            }
        }

        private void FillPitStopWindow(ref Ams2SharedMemory ams2data, SimulatorDataSet simData)
        {
            if (ams2data.mEnforcedPitStopLap <= 0 || simData.SessionInfo.SessionType != SessionType.Race)
            {
                return;
            }

            if (simData.SessionInfo.SessionPhase != SessionPhase.Green && simData.SessionInfo.SessionPhase != SessionPhase.Countdown)
            {
                simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.BeforePitWindow;
                return;
            }

            if (simData.SessionInfo.SessionLengthType == SessionLengthType.Laps)
            {
                if (simData.SessionInfo.LeaderCurrentLap >= simData.SessionInfo.TotalNumberOfLaps)
                {
                    simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.AfterPitWindow;
                    return;
                }

                simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.InPitWindow;
                simData.SessionInfo.PitWindow.PitWindowEnd = simData.SessionInfo.TotalNumberOfLaps - 1;
                return;
            }

            if (simData.SessionInfo.SessionTimeRemaining <= 0)
            {
                simData.SessionInfo.PitWindow.PitWindowState = PitWindowState.AfterPitWindow;
                return;
            }

            TimeSpan sessionDuration = TimeSpan.FromSeconds(ams2data.mSessionDuration);
            TimeSpan simTimeSinceStart = sessionDuration - TimeSpan.FromSeconds(simData.SessionInfo.SessionTimeRemaining);
            TimeSpan differenceBetweenSimAndTracked = LastSessionTime - simTimeSinceStart;

            simData.SessionInfo.PitWindow.PitWindowStart = 1 + differenceBetweenSimAndTracked.TotalMinutes;
            simData.SessionInfo.PitWindow.PitWindowEnd = sessionDuration.TotalMinutes + differenceBetweenSimAndTracked.TotalMinutes;
            simData.SessionInfo.PitWindow.PitWindowState = simTimeSinceStart.TotalMinutes >= 1 ? PitWindowState.InPitWindow : PitWindowState.BeforePitWindow;
        }

        private void FillSpectatingInfo(ref Ams2SharedMemory ams2data, SimulatorDataSet simData)
        {
            switch ((GameState)ams2data.mGameState)
            {
                case GameState.GameExited:
                case GameState.GameFrontEnd:
                case GameState.GameInGamePlaying:
                case GameState.GameInGamePaused:
                case GameState.GameInGameRestarting:
                    simData.SessionInfo.SpectatingState = SpectatingState.Live;
                    break;
                case GameState.GameInGameInMenuTimeTicking:
                    simData.SessionInfo.SpectatingState = ams2data.mViewedParticipantIndex >= 0
                                                          && ams2data.mViewedParticipantIndex < ams2data.mSpeeds.Length &&
                                                          (Math.Abs(ams2data.mSpeed - ams2data.mSpeeds[ams2data.mViewedParticipantIndex]) > 0.1 || ams2data.mCarName.FromArray() !=
                                                              ams2data.mCarNames.FromArray(ams2data.mViewedParticipantIndex * 64)) ? SpectatingState.Spectate : SpectatingState.Live;
                    break;
                case GameState.GameInGameReplay:
                case GameState.GameFrontEndReplay:
                    simData.SessionInfo.SpectatingState = SpectatingState.Replay;
                    break;
                case GameState.GameMax:
                default:
                    simData.SessionInfo.SpectatingState = SpectatingState.Replay;
                    break;
            }
        }

        private void AddPlayerCarInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            CarInfo playerCar = simData.PlayerInfo.CarInfo;

            playerCar.CarDamageInformation.Bodywork.Damage = data.mAeroDamage;
            playerCar.CarDamageInformation.Engine.Damage = data.mEngineDamage;
            playerCar.CarDamageInformation.Suspension.Damage = data.mSuspensionDamage.Max();
            playerCar.CarDamageInformation.Clutch.Damage = data.mClutchWear;
            playerCar.CarDamageInformation.Clutch.Temperature = Temperature.FromKelvin(data.mClutchTemp);
            playerCar.EngineTorque = Torque.FromNm(data.mEngineTorque);
            playerCar.EnginePower = Power.FromKw(data.mEngineTorque * playerCar.EngineRpm / 9549);
            playerCar.TurboPressure = Pressure.FromKiloPascals(data.mTurboBoostPressure / 1000);
            /*playerCar.SpeedLimiterEngaged = (data.mCarFlags & (int)CarFlags.CarSpeedLimiter) == (int)CarFlags.CarSpeedLimiter;*/

            switch ((PitStopSchedule)data.mPitSchedule)
            {
                case PitStopSchedule.PitSchedulePlayerRequested:
                case PitStopSchedule.PitScheduleEngineerRequested:
                case PitStopSchedule.PitScheduleDamageRequested:
                case PitStopSchedule.PitScheduleMandatory:
                case PitStopSchedule.PitScheduleDriveThrough:
                case PitStopSchedule.PitScheduleStopGo:
                    simData.PlayerInfo.PitStopRequested = true;
                    break;
                case PitStopSchedule.PitSchedulePitspotOccupied:
                case PitStopSchedule.PitScheduleNone:
                case PitStopSchedule.PitScheduleMax:
                default:
                    simData.PlayerInfo.PitStopRequested = false;
                    break;
            }

            FillHybridSystem(ref data, playerCar);
            FillBoostData(ref data, playerCar);
            FillDrsData(ref data, playerCar);
            FillSafetySystems(ref data, playerCar);
        }

        private void FillSafetySystems(ref Ams2SharedMemory data, CarInfo playerCar)
        {
            if (data.mAntiLockSetting > -1)
            {
                playerCar.AbsInfo.IsAvailable = true;
                playerCar.AbsInfo.Remark = data.mAntiLockSetting.ToString();
                playerCar.AbsInfo.IsActive = data.mAntiLockActive;
            }

            if (data.mTractionControlSetting > -1)
            {
                playerCar.TcInfo.IsAvailable = true;
                playerCar.TcInfo.Remark = data.mTractionControlSetting.ToString();
                playerCar.TcInfo.IsActive = playerCar.CurrentGear != "N" && Math.Abs(data.mUnfilteredThrottle - data.mThrottle) > 0.01;
            }
        }

        private void FillDrsData(ref Ams2SharedMemory data, CarInfo playerCar)
        {
            DrsState ams2DrsState = (DrsState)data.mDrsState;
            if (ams2DrsState.HasFlag(DrsState.DrsActive))
            {
                playerCar.DrsSystem.DrsStatus = DrsStatus.InUse;
                return;
            }

            if (ams2DrsState.HasFlag(DrsState.DrsAvailableNow))
            {
                playerCar.DrsSystem.DrsStatus = DrsStatus.Available;
                return;
            }

            if (ams2DrsState.HasFlag(DrsState.DrsInstalled))
            {
                playerCar.DrsSystem.DrsStatus = DrsStatus.Equipped;
            }
        }

        private void FillHybridSystem(ref Ams2SharedMemory data, CarInfo playerCar)
        {
            HybridSystem hybrid = playerCar.HybridSystem;
            hybrid.RemainingChargePercentage = data.mBoostAmount / 100.0;
            switch ((ErsDeploymentMode)data.mErsDeploymentMode)
            {
                case ErsDeploymentMode.ERS_DEPLOYMENT_MODE_BUILD:
                    hybrid.HybridSystemMode = HybridSystemMode.Build;
                    break;
                case ErsDeploymentMode.ERS_DEPLOYMENT_MODE_BALANCED:
                    hybrid.HybridSystemMode = HybridSystemMode.Balanced;
                    break;
                case ErsDeploymentMode.ERS_DEPLOYMENT_MODE_ATTACK:
                    hybrid.HybridSystemMode = HybridSystemMode.Attack;
                    break;
                case ErsDeploymentMode.ERS_DEPLOYMENT_MODE_QUAL:
                    hybrid.HybridSystemMode = HybridSystemMode.Qualification;
                    break;
                case ErsDeploymentMode.ERS_DEPLOYMENT_MODE_NONE:
                    hybrid.HybridSystemMode = HybridSystemMode.UnAvailable;
                    break;
                case ErsDeploymentMode.ERS_DEPLOYMENT_MODE_OFF:
                default:
                    hybrid.HybridSystemMode = HybridSystemMode.Off;
                    break;
            }
        }

        private void FillBoostData(ref Ams2SharedMemory data, CarInfo playerCar)
        {
            BoostSystem boostSystem = playerCar.BoostSystem;
            if (data.mErsDeploymentMode > 0)
            {
                return;
            }

            if (data.mBoostAmount <= 0)
            {
                boostSystem.BoostStatus = BoostStatus.UnAvailable;
                return;
            }

            boostSystem.BoostStatus = data.mBoostActive ? BoostStatus.InUse : BoostStatus.Available;

            boostSystem.ActivationsRemaining = (int)data.mBoostAmount;
        }

        public void AddActiveFlags(ref Ams2SharedMemory ams2data, SimulatorDataSet simData)
        {
            if (ams2data.mYellowFlagState != YellowFlagState.YFS_NONE && ams2data.mYellowFlagState != YellowFlagState.YFS_INVALID)
            {
                simData.SessionInfo.SafetyCarState = ams2data.mYellowFlagState switch
                {
                    YellowFlagState.YFS_INVALID => SafetyCarState.None,
                    YellowFlagState.YFS_NONE => SafetyCarState.None,
                    YellowFlagState.YFS_PENDING => SafetyCarState.WaitingForLeader,
                    YellowFlagState.YFS_PITS_CLOSED => SafetyCarState.PitsClosed,
                    YellowFlagState.YFS_PIT_LEAD_LAP => SafetyCarState.PitsOpenForLeadLapCars,
                    YellowFlagState.YFS_PITS_OPEN => SafetyCarState.PitOpen,
                    YellowFlagState.YFS_PITS_OPEN2 => SafetyCarState.PitOpen,
                    YellowFlagState.YFS_LAST_LAP => SafetyCarState.LastScLap,
                    YellowFlagState.YFS_RESUME => SafetyCarState.LightsOff,
                    YellowFlagState.YFS_RACE_HALT => SafetyCarState.None,
                    YellowFlagState.YFS_MAXIMUM => SafetyCarState.None,
                    _ => simData.SessionInfo.SafetyCarState = SafetyCarState.None,
                };

                simData.SessionInfo.ActiveFlags |= FlagKind.FullCourseYellow;
                return;
            }

            for (int i = 0; i < ams2data.mNumParticipants; i++)
            {
                HighestFlagColor highestFlagColor = (HighestFlagColor)ams2data.mHighestFlagColours[i];
                switch (highestFlagColor)
                {
                    case HighestFlagColor.FlagColourNone:
                        continue;
                    case HighestFlagColor.FlagColourYellow:
                    case HighestFlagColor.FlagColourDoubleYellow:
                    {
                        int sector = ams2data.mParticipantData[i].mCurrentSector + 1;
                        switch (sector)
                        {
                            case 1:
                                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
                                break;
                            case 2:
                                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
                                break;
                            case 3:
                                simData.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
                                break;
                        }

                        break;
                    }
                }
            }
        }

        private static void AddAcceleration(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.Acceleration.XinMs = data.mLocalAcceleration[0];
            simData.PlayerInfo.CarInfo.Acceleration.YinMs = data.mLocalAcceleration[1];
            simData.PlayerInfo.CarInfo.Acceleration.ZinMs = -data.mLocalAcceleration[2];
        }

        private static bool IsRxTrack(TrackInfo trackInfo)
        {
            return trackInfo.TrackName.EndsWith("RX");
        }

        private void AddTyresAndFuelInfo(ref Ams2SharedMemory data, SimulatorDataSet simData)
        {
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.mAirPressure[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.mAirPressure[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.mAirPressure[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(data.mAirPressure[(int)WheelIndex.TyreRearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear = Math.Min(1, data.mTyreWear[(int)WheelIndex.TyreFrontLeft] * 2);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear = Math.Min(1, data.mTyreWear[(int)WheelIndex.TyreFrontRight] * 2);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear = Math.Min(1, data.mTyreWear[(int)WheelIndex.TyreRearLeft] * 2);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear = Math.Min(1, data.mTyreWear[(int)WheelIndex.TyreRearRight] * 2);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Rps = data.mTyreRPS[(int)WheelIndex.TyreFrontLeft] * -1; // -6.283185300;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Rps = data.mTyreRPS[(int)WheelIndex.TyreFrontRight] * -1; // -6.283185300;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Rps = data.mTyreRPS[(int)WheelIndex.TyreRearLeft] * -1; //-6.283185300;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Rps = data.mTyreRPS[(int)WheelIndex.TyreRearRight] * -1; //-6.283185300;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel = Distance.FromMeters(data.mSuspensionTravel[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel = Distance.FromMeters(data.mSuspensionTravel[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel = Distance.FromMeters(data.mSuspensionTravel[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel = Distance.FromMeters(data.mSuspensionTravel[(int)WheelIndex.TyreRearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionVelocity = Velocity.FromMs(data.mSuspensionVelocity[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionVelocity = Velocity.FromMs(data.mSuspensionVelocity[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionVelocity = Velocity.FromMs(data.mSuspensionVelocity[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionVelocity = Velocity.FromMs(data.mSuspensionVelocity[(int)WheelIndex.TyreRearRight]);

            int tireDeflatedFlag = (int)TyreFlags.TyreAttached | (int)TyreFlags.TyreInflated;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.Detached = (data.mTyreFlags[(int)WheelIndex.TyreFrontLeft] & tireDeflatedFlag) != tireDeflatedFlag;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.Detached = (data.mTyreFlags[(int)WheelIndex.TyreFrontRight] & tireDeflatedFlag) != tireDeflatedFlag;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.Detached = (data.mTyreFlags[(int)WheelIndex.TyreRearLeft] & tireDeflatedFlag) != tireDeflatedFlag;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.Detached = (data.mTyreFlags[(int)WheelIndex.TyreRearRight] & tireDeflatedFlag) != tireDeflatedFlag;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreType = data.mLFTyreCompoundName.FromArray();
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreType = data.mRFTyreCompoundName.FromArray();
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreType = data.mLRTyreCompoundName.FromArray();
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreType = data.mRRTyreCompoundName.FromArray();

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakesDamage.Damage = data.mBrakeDamage[(int)WheelIndex.TyreFrontLeft];
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakesDamage.MediumDamageThreshold = 0.5;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.BrakesDamage.HeavyDamageThreshold = 0.75;

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakesDamage.Damage = data.mBrakeDamage[(int)WheelIndex.TyreFrontRight];
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakesDamage.MediumDamageThreshold = 0.5;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.BrakesDamage.HeavyDamageThreshold = 0.75;

            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakesDamage.Damage = data.mBrakeDamage[(int)WheelIndex.TyreRearLeft];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakesDamage.MediumDamageThreshold = 0.5;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.BrakesDamage.HeavyDamageThreshold = 0.75;

            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakesDamage.Damage = data.mBrakeDamage[(int)WheelIndex.TyreRearRight];
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakesDamage.MediumDamageThreshold = 0.5;
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.BrakesDamage.HeavyDamageThreshold = 0.75;

            // Front Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(data.mTyreTemp[(int)WheelIndex.TyreFrontLeft]);

            // Front Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(data.mTyreTemp[(int)WheelIndex.TyreFrontRight]);

            // Rear Left Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(data.mTyreTemp[(int)WheelIndex.TyreRearLeft]);

            // Rear Right Tyre Temps
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(data.mTyreTemp[(int)WheelIndex.TyreRearRight]);

            // Fuel System
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(data.mFuelCapacity);
            simData.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(data.mFuelLevel * data.mFuelCapacity);

            simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled = true;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight = Distance.FromCentimeters(data.mRideHeight[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight = Distance.FromCentimeters(data.mRideHeight[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight = Distance.FromCentimeters(data.mRideHeight[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight = Distance.FromCentimeters(data.mRideHeight[(int)WheelIndex.TyreRearRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempLeft[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempRight[(int)WheelIndex.TyreFrontLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempCenter[(int)WheelIndex.TyreFrontLeft]);

            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempLeft[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempRight[(int)WheelIndex.TyreFrontRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempCenter[(int)WheelIndex.TyreFrontRight]);

            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempLeft[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempRight[(int)WheelIndex.TyreRearLeft]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempCenter[(int)WheelIndex.TyreRearLeft]);

            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempLeft[(int)WheelIndex.TyreRearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempRight[(int)WheelIndex.TyreRearRight]);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(data.mTyreTempCenter[(int)WheelIndex.TyreRearRight]);
        }

        internal void AddDriversData(SimulatorDataSet data, ref Ams2SharedMemory ams2data)
        {
            if (ams2data.mNumParticipants < 1)
            {
                return;
            }

            List<DriverInfo> drivers = new List<DriverInfo>(ams2data.mNumParticipants);
            DriverInfo playersInfo = null;

            for (int i = 0; i < ams2data.mNumParticipants; i++)
            {
                ParticipantInfo pcVehicleInfo = ams2data.mParticipantData[i];
                DriverInfo driverInfo = CreateDriverInfo(ref ams2data, pcVehicleInfo, i);
                if (string.IsNullOrWhiteSpace(driverInfo.CarClassId))
                {
                    continue;
                }

                if (driverInfo.CarClassId == "SafetyCar")
                {
                    driverInfo.IsSafetyCar = true;
                    if (driverInfo.InPits && driverInfo.Speed.InKph < 5)
                    {
                        continue;
                    }
                }

                driverInfo.CurrentLapValid = ams2data.mLapsInvalidated[i] == 0;
                drivers.Add(driverInfo);

                if (driverInfo.IsPlayer)
                {
                    driverInfo.CarInfo.SpeedLimiterEngaged = ((CarFlags)ams2data.mCarFlags).HasFlag(CarFlags.CarSpeedLimiter);
                    driverInfo.CarInfo.HeadLightsStatus = ((CarFlags)ams2data.mCarFlags).HasFlag(CarFlags.CarHeadlight) ? HeadLightsStatus.NormalBeams : HeadLightsStatus.Off;
                    playersInfo = driverInfo;
                    _lastPlayerShortName = driverInfo.DriverShortName;
                }

                if (driverInfo.Position == 1)
                {
                    data.SessionInfo.LeaderCurrentLap = driverInfo.CompletedLaps + 1;
                    data.LeaderInfo = driverInfo;
                }

                AddLappingInformation(data, ref ams2data, driverInfo);
                FillTimingInfo(driverInfo, pcVehicleInfo, ref ams2data, i);

                if (data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector1) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector2) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector3))
                {
                    driverInfo.IsCausingYellow = !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 40;
                }

                if (driverInfo.IsPlayer)
                {
                    FillYellowFlagBySurface(data, driverInfo, ref ams2data);
                }
            }

            data.DriversInfo = drivers.ToArray();
            _lastPlayer = playersInfo;

            if (playersInfo != null)
            {
                data.PlayerInfo = playersInfo;
            }
        }

        private void FillYellowFlagBySurface(SimulatorDataSet data, DriverInfo driverInfo, ref Ams2SharedMemory ams2data)
        {
            if (IsRxTrack(data.SessionInfo.TrackInfo) || driverInfo.InPits || ams2data.mTerrain.Any(x => !IsOffRoad((TerrainType)x)))
            {
                return;
            }

            driverInfo.IsCausingYellow = true;
            switch (driverInfo.Timing.CurrentSector)
            {
                case 1:
                    data.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
                    break;
                case 2:
                    data.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
                    break;
                case 3:
                    data.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
                    break;
            }
        }

        private bool IsOffRoad(TerrainType terrainType)
        {
            return terrainType is TerrainType.TerrainGrass or TerrainType.TerrainGravel or TerrainType.TerrainSand;
        }

        internal void FillTimingInfo(DriverInfo driverInfo, ParticipantInfo pcVehicleInfo, ref Ams2SharedMemory ams2SharedMemory, int vehicleIndex)
        {
            driverInfo.Timing.LastSector1Time = CreateTimeSpan(ams2SharedMemory.mCurrentSector1Times[vehicleIndex]);
            driverInfo.Timing.LastSector2Time = CreateTimeSpan(ams2SharedMemory.mCurrentSector2Times[vehicleIndex]);
            driverInfo.Timing.LastSector3Time = CreateTimeSpan(ams2SharedMemory.mCurrentSector3Times[vehicleIndex]);
            driverInfo.Timing.LastLapTime = CreateTimeSpan(ams2SharedMemory.mLastLapTimes[vehicleIndex]);
            driverInfo.Timing.CurrentSector = pcVehicleInfo.mCurrentSector + 1;
            driverInfo.Timing.CurrentLapTime = TimeSpan.Zero;
        }

        private void AddLappingInformation(SimulatorDataSet data, ref Ams2SharedMemory ams2data, DriverInfo driverInfo)
        {
            if (data.SessionInfo.SessionType == SessionType.Race && _lastPlayer != null
                && _lastPlayer.CompletedLaps != 0)
            {
                driverInfo.IsBeingLappedByPlayer = driverInfo.TotalDistance < (_lastPlayer.TotalDistance - (ams2data.mTrackLength * 0.5));
                driverInfo.IsLappingPlayer = _lastPlayer.TotalDistance < (driverInfo.TotalDistance - (ams2data.mTrackLength * 0.5));
            }
        }

        private DriverInfo CreateDriverInfo(ref Ams2SharedMemory ams2data, ParticipantInfo pcVehicleInfo, int vehicleIndex)
        {
            DriverInfo driverInfo = new DriverInfo
            {
                CompletedLaps = (int)pcVehicleInfo.mLapsCompleted,
                CarName = ams2data.mCarNames.FromArray(vehicleIndex * 64),
                CarClassName = ams2data.mCarClassNames.FromArray(vehicleIndex * 64),
                InPits = ams2data.mPitModes[vehicleIndex] != 0
            };

            if (_customClassMap.TryGetValue(driverInfo.CarName, out string customClass))
            {
                driverInfo.CarClassName = customClass;
            }

            driverInfo.DriverShortName = driverInfo.DriverLongName = pcVehicleInfo.mName.FromArray();
            driverInfo.CarClassId = driverInfo.CarClassName;
            driverInfo.IsPlayer = DriverInfoIsPlayer(ams2data, vehicleIndex, driverInfo);
            driverInfo.Position = (int)pcVehicleInfo.mRacePosition;
            driverInfo.Speed = Velocity.FromMs(ams2data.mSpeeds[vehicleIndex]);
            driverInfo.LapDistance = pcVehicleInfo.mCurrentLapDistance;
            driverInfo.TotalDistance = (pcVehicleInfo.mLapsCompleted * ams2data.mTrackLength) + driverInfo.LapDistance;
            driverInfo.FinishStatus = FromPCarStatus((RaceState)ams2data.mRaceStates[vehicleIndex]);
            driverInfo.WorldPosition = new Point3D(Distance.FromMeters(-pcVehicleInfo.mWorldPosition[0]), Distance.FromMeters(pcVehicleInfo.mWorldPosition[1]), Distance.FromMeters(pcVehicleInfo.mWorldPosition[2]));
            driverInfo.DriverSessionId = driverInfo.DriverLongName + driverInfo.CarClassId;
            ComputeDistanceToPlayer(_lastPlayer, driverInfo, ams2data.mTrackLength);
            return driverInfo;
        }

        private bool DriverInfoIsPlayer(Ams2SharedMemory ams2data, int vehicleIndex, DriverInfo driverInfo)
        {
            return (vehicleIndex == ams2data.mViewedParticipantIndex 
                   && driverInfo.CarName == ams2data.mCarName.FromArray()
                   && ams2data.mViewedParticipantIndex < ams2data.mSpeeds.Length && (Math.Abs(ams2data.mSpeed - ams2data.mSpeeds[ams2data.mViewedParticipantIndex]) < 0.1))
                || driverInfo.DriverShortName == _lastPlayerShortName;
        }

        internal void FillSessionInfo(ref Ams2SharedMemory data, SimulatorDataSet simData, TimeSpan sessionTime)
        {
            // Timing
            simData.SessionInfo.SessionTime = sessionTime;
            simData.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(data.mTrackLength);
            simData.SessionInfo.TrackInfo.TrackName = data.mTranslatedTrackLocation.FromArray();
            simData.SessionInfo.TrackInfo.TrackLayoutName = data.mTranslatedTrackVariation.FromArray();
            simData.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(data.mAmbientTemperature);
            simData.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(data.mTrackTemperature);
            simData.SessionInfo.WeatherInfo.RainIntensity = (int)(Math.Max(data.mRainDensity, data.mSnowDensity) * 100.0);
            simData.SessionInfo.WeatherInfo.HasWindInformation = true;
            simData.SessionInfo.WeatherInfo.WindSpeed = Velocity.FromMs(data.mWindSpeed);
            simData.SessionInfo.WeatherInfo.WindDirectionFrom = Angle.GetFromRadians(Math.Atan2(data.mWindDirectionY, data.mWindDirectionX)).InDegrees - 90;
            simData.SessionInfo.IsMultiplayer = data.mNationalities.Any(x => x > 0);

            simData.SessionInfo.SessionType = (Ams2SessionType)data.mSessionState switch
            {
                Ams2SessionType.SessionInvalid => SessionType.Na,
                Ams2SessionType.SessionPractice => SessionType.Practice,
                Ams2SessionType.SessionTest => SessionType.Practice,
                Ams2SessionType.SessionQualify => SessionType.Qualification,
                Ams2SessionType.SessionFormationLap => SessionType.Race,
                Ams2SessionType.SessionRace => SessionType.Race,
                Ams2SessionType.SessionTimeAttack => SessionType.Practice,
                Ams2SessionType.SessionMax => SessionType.Na,
                _ => simData.SessionInfo.SessionType
            };

            simData.SessionInfo.SessionPhase = (GameState)data.mGameState switch
            {
                GameState.GameExited => SessionPhase.Countdown,
                GameState.GameFrontEnd => SessionPhase.Countdown,
                GameState.GameInGamePlaying => SessionPhase.Green,
                GameState.GameInGamePaused => SessionPhase.Green,
                GameState.GameInGameInMenuTimeTicking => SessionPhase.Green,
                GameState.GameInGameRestarting => SessionPhase.Checkered,
                GameState.GameInGameReplay => SessionPhase.Green,
                GameState.GameFrontEndReplay => SessionPhase.Green,
                GameState.GameMax => SessionPhase.Unavailable,
                _ => SessionPhase.Unavailable
            };

            simData.SessionInfo.IsActive = simData.SessionInfo.SessionType != SessionType.Na;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (data.mSessionDuration > 0)
            {
                simData.SessionInfo.SessionLengthType = data.mSessionAdditionalLaps == 0 || simData.SessionInfo.SessionType != SessionType.Race ? SessionLengthType.Time : SessionLengthType.TimeWithExtraLap;
                simData.SessionInfo.SessionTimeRemaining = Math.Max(data.mEventTimeRemaining, 0);
            }
            else if (data.mLapsInEvent != 0)
            {
                simData.SessionInfo.SessionLengthType = SessionLengthType.Laps;
                simData.SessionInfo.TotalNumberOfLaps = (int)data.mLapsInEvent;
            }
        }

        private void InitializeCustomClassMap()
        {
            if (_ams2Configuration.SplitVintageTouringTier1)
            {
                _customClassMap.Add("Lamborghini Miura SV", "TC60S (Miura)");
                _customClassMap.Add("Chevrolet Corvette C3", "TC60S (Corvette)");
                _customClassMap.Add("BMW 2002 Turbo", "TC60S (BMW 2002)");
            }

            if (_ams2Configuration.SplitVintageTouringTier2)
            {
                _customClassMap.Add("Lotus 23", "TC60S2 (Lotus 23)");
                _customClassMap.Add("MINI Cooper S 1965", "TC60S2 (Mini)");
            }

            if (_ams2Configuration.SplitTsiCup)
            {
                _customClassMap.Add("Volkswagen Virtus", "TSICup");
                _customClassMap.Add("Volkswagen Polo", "TSICup");

                _customClassMap.Add("Volkswagen Virtus GTS", "TSICup GTS");
                _customClassMap.Add("Volkswagen Polo GTS", "TSICup GTS");
            }

            if (_ams2Configuration.SplitCarreraCup)
            {
                _customClassMap.Add("Porsche 911 GT3 Cup 4.0", "Carrera Cup 4.0");
                _customClassMap.Add("Porsche 911 GT3 Cup 3.8", "Carrera Cup 3.8");
            }

            if (_ams2Configuration.SplitLancerCup)
            {
                _customClassMap.Add("Mitsubishi Lancer R", "LancerCup");
                _customClassMap.Add("Mitsubishi Lancer RS", "LancerCup RS");
            }

            if (_ams2Configuration.SplitGtClassics)
            {
                _customClassMap.Add("Chevrolet Corvette C3-R", "TC70S (Corvette)");
                _customClassMap.Add("Porsche 911 RSR 74", "TC70S (911 RSR 74)");
            }
        }
    }
}
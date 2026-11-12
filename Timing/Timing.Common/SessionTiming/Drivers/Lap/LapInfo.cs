namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using NLog;

    using SecondMonitor.DataModel;

    using Telemetry;
    using ViewModels.Settings;
    using ViewModels.Settings.ViewModel;

    public class LapInfo : ILapInfo
    {
        private static readonly TimeSpan maxPendingTime = TimeSpan.FromSeconds(2);
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private bool _captureTelemetry;
        private bool _valid;
        private bool _completed;
        private bool _isPending;
        private TimeSpan _isPendingStart;

        public LapInfo(SimulatorDataSet dataSet, int lapNumber, DriverTiming driver, ILapInfo previousLapInfo, ISettingsProvider settingsProvider)
            : this(dataSet, lapNumber, driver, false, previousLapInfo, settingsProvider)
        {
        }

        public LapInfo(SimulatorDataSet dataSet, int lapNumber, DriverTiming driver, bool firstLap, ILapInfo previousLapInfo, ISettingsProvider settingsProvider)
        {
            LapGuid = Guid.NewGuid();
            logger.Info($"Driver {driver.DriverId} is starting Lap {lapNumber}");
            Driver = driver;
            LapStartSessionTime = dataSet.SessionInfo.SessionTime;
            LapProgressTimeBySim = TimeSpan.Zero;
            LapProgressTimeByTiming = TimeSpan.Zero;
            LapNumber = lapNumber;
            Valid = true;
            FirstLap = firstLap;
            IsPitLap = false;
            PreviousLap = previousLapInfo;
            CompletedDistance = double.NaN;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _captureTelemetry = ShouldCaptureTelemetry();
            LapTelemetryInfo = new LapTelemetryInfo(driver.DriverInfo, dataSet, this, TimeSpan.FromMilliseconds(_displaySettingsViewModel.TelemetrySettingsViewModel.LoggingInterval), dataSet.SimulatorSourceInfo, _captureTelemetry);
            StintNumber = driver.PitStops.Count + 1;
        }

        public event EventHandler<SectorCompletedArgs> SectorCompletedEvent;
        public event EventHandler<LapEventArgs> LapInvalidatedEvent;
        public event EventHandler<LapEventArgs> LapCompletedEvent;

        public static Func<ILapInfo, SectorTiming> Sector1SelFunc => x => x.Sector1;
        public static Func<ILapInfo, SectorTiming> Sector2SelFunc => x => x.Sector2;
        public static Func<ILapInfo, SectorTiming> Sector3SelFunc => x => x.Sector3;

        public int LapEndPosition { get; private set; }
        public int LapEndPositionInClass { get; private set; }

        public LapCompletionMethod LapCompletionMethod { get; set; } = LapCompletionMethod.None;

        public Guid LapGuid { get; }

        public TimeSpan LapStartSessionTime { get; }

        public TimeSpan LapEndSessionTime { get; set; }

        public int LapNumber { get; private set; }

        public double CompletedDistance { get; private set; }
        public int StintNumber { get; }
        
        public bool IsSafetyCarLap { get; private set; }

        public LapInvalidationReasonKind LapInvalidationReasonKind { get; private set; }

        public bool Valid
        {
            get => _valid;
            private set
            {
                if (Valid && !value)
                {
                    _valid = false;
                    OnLapInvalidatedEvent(new LapEventArgs(this));
                }
                else
                {
                    _valid = value;
                }
            }
        }

        public DriverTiming Driver { get; }

        public bool FirstLap { get; }

        public bool InvalidBySim { get; set; }

        public bool IsPitLap { get; private set; }
        public bool IsPitEntryLap { get; private set; }

        public bool IsPitExitLap => Driver.PitStops.Any(x => x.ExitLap == this);

        public bool Completed
        {
            get => _completed;
            private set
            {
                _completed = value;
                if (Completed)
                {
                    OnLapCompletedEvent(new LapEventArgs(this));
                }
            }
        }

        public ILapInfo PreviousLap { get; }

        public SectorTiming Sector1 { get; private set; }

        public SectorTiming Sector2 { get; private set; }

        public SectorTiming Sector3 { get; private set; }

        public TimeSpan LapEnd { get; private set; }

        public TimeSpan LapTime { get; private set; } = TimeSpan.Zero;

        public TimeSpan LapProgressTimeByTiming { get; private set; }

        private bool LapProgressTimeBySimInitialized { get; set; }

        public TimeSpan LapProgressTimeBySim { get; private set; }

        public TimeSpan CurrentlyValidProgressTime =>
            LapProgressTimeBySimInitialized ? LapProgressTimeBySim : LapProgressTimeByTiming;

        public SectorTiming CurrentSector { get; private set; }

        private PendingSector PendingSector { get; set; }

        public bool IsPending => _isPending || PendingSector != null;

        public LapTelemetryInfo LapTelemetryInfo { get; }

        public void FinishLap(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            TimeSpan lapDurationByTiming = dataSet.SessionInfo.SessionTime.Subtract(LapStartSessionTime);
            LapEndSessionTime = dataSet.SessionInfo.SessionTime;

            // Perform a sanity check on the sim reported lap time. The time difference between what the application counted and the sim counted cannot be more than 15 seconds.
            if (!dataSet.SimulatorSourceInfo.HasLapTimeInformation || (driverInfo.Timing.LastLapTime == TimeSpan.Zero && dataSet.SessionInfo.SessionType != SessionType.Na) || ShouldForceUseAppTime(dataSet, driverInfo)) // || Math.Abs(lapDurationByTiming.TotalSeconds - driverInfo.Timing.LastLapTime.TotalSeconds) > 15)
            {
                LapEnd = _isPendingStart != TimeSpan.Zero ? _isPendingStart : dataSet.SessionInfo.SessionTime;
            }
            else
            {
                LapEnd = LapStartSessionTime.Add(driverInfo.Timing.LastLapTime);
            }

            LapTime = LapEnd.Subtract(LapStartSessionTime);
            SectorTiming[] sectors = { Sector1, Sector2, Sector3 };
            if (LapTime == TimeSpan.Zero)
            {
                InvalidateLap(LapInvalidationReasonKind.NoValidLapTime);
            }

            if (CompletedDistance < dataSet.SessionInfo.TrackInfo.LayoutLength.InMeters * 0.8)
            {
                InvalidateLap(LapInvalidationReasonKind.CompletedDistanceLessThanLapThreshold);
            }

            if (sectors.Any(x => x?.Duration != TimeSpan.Zero) && sectors.Any(x => x == null || x.Duration == TimeSpan.Zero) && dataSet.SimulatorSourceInfo.SectorTimingSupport == DataInputSupport.Full)
            {
                InvalidateLap(LapInvalidationReasonKind.NotAllSectorHaveTime);
            }

            LapTelemetryInfo.CreateLapEndSnapshot(driverInfo, dataSet.SessionInfo.WeatherInfo, dataSet.InputInfo, dataSet.SimulatorSourceInfo);

            if (Sector3?.IsFinished == false && dataSet.SimulatorSourceInfo.SectorTimingSupport == DataInputSupport.Full)
            {
                FinishSector(Sector3, driverInfo, dataSet);
            }

            LapTelemetryInfo.Complete(dataSet.SessionInfo.TrackInfo.LayoutLength);
            Completed = true;
        }

        private bool ShouldForceUseAppTime(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            return dataSet.SessionInfo.SessionType == SessionType.Race && LapNumber == 1 && dataSet.Source == SimulatorsNameMap.AccSimName;
        }

        public void Tick(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            IsPitLap |= driverInfo.InPits;

            if (!IsPitEntryLap && Driver.InPits && Driver.LastPitStop.EntryLap == this)
            {
                IsPitEntryLap = true;
            }

            IsSafetyCarLap |= dataSet.SessionInfo.SafetyCarState != SafetyCarState.None;

            UpdateLapProgressTimeBySim(dataSet, driverInfo);
            LapProgressTimeByTiming = dataSet.SessionInfo.SessionTime.Subtract(LapStartSessionTime);
            LapEndPosition = driverInfo.Position;
            LapEndPositionInClass = driverInfo.PositionInClass;

            // Let 5 seconds for the source data noise, when lap count might not be properly updated at instance creation
            if (LapProgressTimeByTiming.TotalSeconds < 5 && LapNumber != driverInfo.CompletedLaps + 1)
            {
                LapNumber = driverInfo.CompletedLaps + 1;
            }

            if (dataSet.SimulatorSourceInfo.SectorTimingSupport != DataInputSupport.None)
            {
                TickSectors(dataSet, driverInfo);
            }

/*            if (IsMaxSpeedViolated(driverInfo))
            {
                InvalidateLap(LapInvalidationReasonKind.SanityMaxSpeedViolated);
            }*/

            // driverInfo.TraveledDistance might still hold value from previous lap at this point, so wait until it is reasonably small before starting to compute complete distance.
            // Allow 90% of layout length, as some AC tracks have pit exit before the lap end.
            if (double.IsNaN(CompletedDistance) && driverInfo.LapDistance < dataSet.SessionInfo.TrackInfo.LayoutLength.InMeters * 0.9)
            {
                CompletedDistance = driverInfo.LapDistance;
            }

            if (double.IsNaN(CompletedDistance))
            {
                return;
            }

            if (CompletedDistance <= driverInfo.LapDistance)
            {
                CompletedDistance = driverInfo.LapDistance;
            }

            _captureTelemetry &= ShouldCaptureTelemetry();
            LapTelemetryInfo.UpdateTelemetry(dataSet, driverInfo, _captureTelemetry);
        }

        private void UpdateLapProgressTimeBySim(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            TimeSpan newLapProgressTimeBySim = driverInfo.Timing.CurrentLapTime;

            // sim is reporting nonsense as our current lap time
            if (newLapProgressTimeBySim <= TimeSpan.Zero)
            {
                LapProgressTimeBySimInitialized = false;
                return;
            }

            // We were unable to use the sim provided lap time for 75% for the lap. I thing it is safe to say that this boat has passed
            if (!LapProgressTimeBySimInitialized && CompletedDistance > dataSet.SessionInfo.TrackInfo.LayoutLength.InMeters * 0.75)
            {
                return;
            }

            // It is not possible to use the lap time provided by the sim from the beginning, because it might still contain value from the previous lap. The sim has 5 second window to update the value to correct time
            // Only after that is done we can use the lap provided lap time
            if (!LapProgressTimeBySimInitialized && newLapProgressTimeBySim < TimeSpan.FromSeconds(5))
            {
                LapProgressTimeBySim = newLapProgressTimeBySim;
                LapProgressTimeBySimInitialized = true;
                return;
            }

            // Sim reported lap time is smaller than the previously reported lap time, that doesn't sound good - we're unable to use the lap time. But only if sim doesn't offer rewind functionality.
            if (LapProgressTimeBySimInitialized && newLapProgressTimeBySim + TimeSpan.FromSeconds(5) < CurrentlyValidProgressTime && !dataSet.SimulatorSourceInfo.HasRewindFunctionality)
            {
                LapProgressTimeBySimInitialized = false;
                return;
            }

            // Huuray, sanity checks met, we're able to use the lap time provided by sim
            if (LapProgressTimeBySimInitialized)
            {
                LapProgressTimeBySim = newLapProgressTimeBySim;
            }
        }

        private void TickSectors(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            if (dataSet.SimulatorSourceInfo.SectorTimingSupport == DataInputSupport.PlayerOnly && !driverInfo.IsPlayer)
            {
                return;
            }

            if (CurrentSector == null && driverInfo.Timing.CurrentSector != 1)
            {
                return;
            }

            if (CurrentSector == null && driverInfo.Timing.CurrentSector == 1)
            {
                Sector1 = new SectorTiming(1, dataSet, this);
                CurrentSector = Sector1;
                return;
            }

            if (CurrentSector == null)
            {
                return;
            }

            if (driverInfo.Timing.CurrentSector == 0)
            {
                return;
            }

            UpdateSectorPendingState(dataSet, driverInfo);

            SectorTiming shouldBeActive = PickSector(driverInfo.Timing.CurrentSector);
            if (shouldBeActive == CurrentSector)
            {
                CurrentSector.Tick(dataSet, driverInfo);
                return;
            }

            //F1 2019 Fix : Because F1 2019 has flashback functionality, we have to check if the sector change was in forward or backward direction
            if (CurrentSector.SectorNumber % 3 == driverInfo.Timing.CurrentSector - 1)
            {
                FinishSectorAndCheckIfPending(CurrentSector, driverInfo, dataSet);
            }

            switch (driverInfo.Timing.CurrentSector)
            {
                case 2:
                    Sector2 = new SectorTiming(2, dataSet, this);
                    CurrentSector = Sector2;
                    return;
                case 3:
                    Sector3 = new SectorTiming(3, dataSet, this);
                    CurrentSector = Sector3;
                    return;
            }
        }

        private void FinishSectorAndCheckIfPending(
            SectorTiming sectorTiming,
            DriverInfo driverInfo,
            SimulatorDataSet dataSet)
        {
            if (SectorTiming.PickTimingFormDriverInfo(driverInfo, sectorTiming.SectorNumber) <= TimeSpan.Zero)
            {
                InitPendingSector(CurrentSector, driverInfo, dataSet);
            }
            else
            {
                FinishSector(sectorTiming, driverInfo, dataSet);
            }
        }

        private void FinishSector(SectorTiming sectorTiming, DriverInfo driverInfo, SimulatorDataSet dataSet)
        {
            sectorTiming.Finish(driverInfo, dataSet);
            if ((sectorTiming.Duration == TimeSpan.Zero || (Driver.InPits && dataSet.SessionInfo.SessionType != SessionType.Race)) && dataSet.SimulatorSourceInfo.InvalidateLapBySector)
            {
                InvalidateLap(LapInvalidationReasonKind.InvalidatedBySectorTime);
            }

            OnSectorCompleted(new SectorCompletedArgs(sectorTiming));
        }

        private void InitPendingSector(SectorTiming sectorTiming, DriverInfo driverInfo, SimulatorDataSet dataSet)
        {
            if (PendingSector?.Sector == sectorTiming)
            {
                return;
            }

            if (PendingSector != null)
            {
                FinishSector(PendingSector.Sector, driverInfo, dataSet);
            }

            PendingSector = new PendingSector(sectorTiming, dataSet.SessionInfo.SessionTime);
        }

        // R3E has some weird behavior that sometimes it doesn't report last lap time correctly (reports zero), do not end lap in such weird cases
        public bool SwitchToPendingIfNecessary(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            if (driverInfo.FinishStatus == DriverFinishStatus.Dnf || driverInfo.FinishStatus == DriverFinishStatus.Dq)
            {
                return false;
            }

            if (dataSet.SimulatorSourceInfo.ForceLapOverTime)
            {
                logger.Info("Pending Reason: Force Lap Overtime");
                _isPending = true;
                _isPendingStart = dataSet.SessionInfo.SessionTime;
            }

            if (dataSet.SimulatorSourceInfo.HasLapTimeInformation && driverInfo.Timing.LastLapTime == TimeSpan.Zero)
            {
                logger.Info("Pending Reason: Last Lap Time is Zero");
                _isPending = true;
                _isPendingStart = dataSet.SessionInfo.SessionTime;
            }

            if (PreviousLap != null && dataSet.SimulatorSourceInfo.HasLapTimeInformation && (driverInfo.Timing.LastLapTime == PreviousLap.LapTime || PreviousLap.LapTime == TimeSpan.Zero))
            {
                logger.Info("Previous lap time same as this");
                _isPending = true;
                _isPendingStart = dataSet.SessionInfo.SessionTime;
            }

            return IsPending;
        }

        public void UpdateSectorPendingState(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            if (PendingSector != null && (PendingSector.HasValidTimingInformation(driverInfo) || PendingSector.HasTimedOut(dataSet.SessionInfo.SessionTime)))
            {
                FinishSector(PendingSector.Sector, driverInfo, dataSet);
                PendingSector = null;
            }
        }

        public bool UpdatePendingState(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
            if (PendingSector != null && (PendingSector.HasValidTimingInformation(driverInfo) || PendingSector.HasTimedOut(dataSet.SessionInfo.SessionTime)))
            {
                FinishSector(PendingSector.Sector, driverInfo, dataSet);
                PendingSector = null;
            }

            if (dataSet.SessionInfo.SessionTime - _isPendingStart > maxPendingTime)
            {
                _isPending = false;
                return IsPending;
            }

            if (dataSet.SimulatorSourceInfo.ForceLapOverTime)
            {
                return IsPending;
            }

            if (_isPending && (driverInfo.Timing.LastLapTime == TimeSpan.Zero))
            {
                return IsPending;
            }

            if (PreviousLap != null && _isPending && (driverInfo.Timing.LastLapTime == PreviousLap.LapTime && PreviousLap.LapTime == TimeSpan.Zero))
            {
                return IsPending;
            }

            _isPending = false;
            return IsPending;
        }

        /*
         * Performs a sanity check on the laps data to evaluate if the lap really make sense (i.e. if it wasn't terminated prematurely by the sim
         */
        public bool IsLapDataSane(SimulatorDataSet dataSet)
        {
            // Not completed laps are always sane
            if (!Completed)
            {
                return true;
            }

            // Lap data is sane if we completed at least 75% of the track and the lap hes run for more than 10 seconds
            return CompletedDistance > dataSet.SessionInfo.TrackInfo.LayoutLength.InMeters * 0.75 && LapProgressTimeByTiming > TimeSpan.FromSeconds(10);
        }

        public void OverrideTime(TimeSpan overrideLapTime, bool forceValid)
        {
            logger.Info($"Overriding lap time for Driver {Driver.DriverId}, Old Time {LapTime.ToString()}, New Time: {overrideLapTime.ToString()}, Lap Numer {LapNumber}");
            LapTime = overrideLapTime;
            Valid |= forceValid;
        }

        public void InvalidateLap(LapInvalidationReasonKind lapInvalidationReasonKind)
        {
            if (!Valid)
            {
                return;
            }

            LapInvalidationReasonKind = lapInvalidationReasonKind;
            Valid = false;
            logger.Info($"Driver : {Driver.DriverInfo.DriverSessionId}, Invalidated lap {LapNumber}, , REASON : {lapInvalidationReasonKind.ToString()} ");
        }

        private SectorTiming PickSector(int sectorNumber)
        {
            switch (sectorNumber)
            {
                case 1:
                    return Sector1;
                case 2:
                    return Sector2;
                case 3:
                    return Sector3;
                default:
                    return null;
            }
        }

        protected virtual void OnSectorCompleted(SectorCompletedArgs e)
        {
            SectorCompletedEvent?.Invoke(this, e);
        }

        protected virtual void OnLapInvalidatedEvent(LapEventArgs e)
        {
            LapInvalidatedEvent?.Invoke(this, e);
        }

        protected virtual void OnLapCompletedEvent(LapEventArgs e)
        {
            LapCompletedEvent?.Invoke(this, e);
        }

        private bool ShouldCaptureTelemetry()
        {
            var telemetrySettings = _displaySettingsViewModel.TelemetrySettingsViewModel;
            var sessionType = Driver.Session.SessionType;
            if (Driver.IsPlayer)
            {
                return true;
            }

            if (!telemetrySettings.IsTelemetryLoggingEnabled || (!telemetrySettings.LogOpponentsBest && !telemetrySettings.LogBestForEachDriver && !telemetrySettings.LogOpponentsAll) || Driver.PositionInClass > telemetrySettings.LogOnlyTop)
            {
                return false;
            }

            return telemetrySettings.IsTelemetryEnabledForSession(sessionType);
        }
    }
}
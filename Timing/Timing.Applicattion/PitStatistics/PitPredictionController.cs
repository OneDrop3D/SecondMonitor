namespace SecondMonitor.Timing.Application.PitStatistics
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Common.SessionTiming.Drivers;
    using Contracts.Session;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker;

    using ViewModels.CarStatus;
    using ViewModels.CarStatus.FuelStatus;
    using ViewModels.Controllers;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;

    public class PitPredictionController : IController, IPlayerPitEstimationProvider
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly IFuelPredictionProvider _fuelPredictionProvider;
        private readonly IPitStopStatisticProvider _pitStopStatisticProvider;
        private readonly IDriverTimingProvider _driverTimingProvider;
        private readonly ISettingsProvider _settingsProvider;
        private Timer _timer;
        private bool _inTimer;

        public PitPredictionController(ISessionEventProvider sessionEventProvider, SessionRemainingCalculator sessionRemainingCalculator, IFuelPredictionProvider fuelPredictionProvider,
            IPitStopStatisticProvider pitStopStatisticProvider, IDriverTimingProvider driverTimingProvider, ISettingsProvider settingsProvider)
        {
            _sessionRemainingCalculator = sessionRemainingCalculator;
            _fuelPredictionProvider = fuelPredictionProvider;
            _pitStopStatisticProvider = pitStopStatisticProvider;
            _driverTimingProvider = driverTimingProvider;
            _settingsProvider = settingsProvider;
            _sessionEventProvider = sessionEventProvider;
            CurrentPitReturnPrediction = PitReturnPrediction.Empty;
        }

        public bool IsPlayerExpectedToPit => PitPredictionReason != PitPredictionReason.None;
        public bool IsPredictedReturnPositionFilled { get; set; }
        public PitReturnPrediction CurrentPitReturnPrediction { get; set; }

        public PitPredictionReason PitPredictionReason { get; private set; }
        public void Reset()
        {
            PitPredictionReason = PitPredictionReason.None;
        }

        public Task StartControllerAsync()
        {
            _timer = new Timer((_) => UpdatePlayerExpectedToPit(), null, 1000, 1000);
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _timer.Dispose();
            return Task.CompletedTask;
        }

        private void UpdatePlayerExpectedToPit()
        {
            if (_inTimer)
            {
                return;
            }

            _inTimer = true;
            var datasetToUse = _sessionEventProvider.LastDataSet;

            if (!_driverTimingProvider.TryGetPlayerTiming(out DriverTiming playerTiming))
            {
                _inTimer = false;
                return;
            }

            if (!IsDataSetValid(datasetToUse))
            {
                PitPredictionReason = PitPredictionReason.None;
                _inTimer = false;
                return;
            }

            PitPredictionReason = (IsAlwaysEnabled() | IsPitStopRequested(datasetToUse) | IsInPitWindow(datasetToUse, playerTiming) | IsPitStopByTyre(datasetToUse) | IsPitStopByFuel(datasetToUse) | IsByStintEnding(datasetToUse));

            if (IsPlayerExpectedToPit)
            {
                UpdateReturnPosition(playerTiming);
            }
            else
            {
                IsPredictedReturnPositionFilled = false;
            }

            _inTimer = false;
        }

        private PitPredictionReason IsAlwaysEnabled()
        {
            if (_settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel.AlwaysEnabled.IsMapEnabled || _settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel.AlwaysEnabled.IsPitBoardEnabled)
            {
                return PitPredictionReason.AlwaysEnabled;
            }

            return PitPredictionReason.None;
        }

        private void UpdateReturnPosition(DriverTiming playerTiming)
        {
            var pitStatistics = _pitStopStatisticProvider.GetCurrentStatisticsPlayerClass();
            var pitSettings = _settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel;
            if (pitStatistics.EntriesCount == 0 && !(pitSettings.IsOverridePitStallTimeEnabled && pitSettings.IsOverridePitLaneTimeEnabled))
            {
                IsPredictedReturnPositionFilled = false;
                return;
            }

            TimeSpan pitLaneLostTime = pitSettings.IsOverridePitLaneTimeEnabled ? TimeSpan.FromSeconds(pitSettings.OverridePitLaneTime) : pitStatistics.TotalTimeLost - pitStatistics.PitStallDuration;
            TimeSpan pitStallLost = pitSettings.IsOverridePitStallTimeEnabled ? TimeSpan.FromSeconds(pitSettings.OverridePitStallTime) : pitStatistics.PitStallDuration;

            TimeSpan expectedPitTime = pitLaneLostTime + pitStallLost + TimeSpan.FromSeconds(pitSettings.ExtraTime);
            if (playerTiming.InPits)
            {
                expectedPitTime -= playerTiming.LastPitStop.PitStopDuration;
            }

            SectionRecord sectionRecord = playerTiming.DriverLapSectorsTracker.GetSectionRecordBeforeTime(expectedPitTime);

            DriverTiming[] allDriversOrdered = _driverTimingProvider.GetAll().Where(x => !x.IsPlayer && x.DriverInfo.FinishStatus == DriverFinishStatus.None).OrderBy(x => x.TotalDistanceTraveled).ToArray();
            DriverTiming nextOverall = allDriversOrdered.FirstOrDefault(x => x.TotalDistanceTraveled > sectionRecord.TotalDistance);
            DriverTiming previousOverall = allDriversOrdered.LastOrDefault(x => x.TotalDistanceTraveled < sectionRecord.TotalDistance);

            DriverTiming driverInFrontClass = allDriversOrdered.Where(x => x.CarClassId == playerTiming.CarClassId).FirstOrDefault(x => x.TotalDistanceTraveled > sectionRecord.TotalDistance);
            DriverTiming driverBehindInClass = allDriversOrdered.Where(x => x.CarClassId == playerTiming.CarClassId).LastOrDefault(x => x.TotalDistanceTraveled < sectionRecord.TotalDistance);

            ReturnPosition classReturnPosition = CreateReturnPosition(playerTiming, sectionRecord, driverInFrontClass, driverBehindInClass, true);
            ReturnPosition overallReturnPosition = CreateReturnPosition(playerTiming, sectionRecord, nextOverall, previousOverall, false);

            CurrentPitReturnPrediction = new PitReturnPrediction(classReturnPosition, overallReturnPosition,
                sectionRecord.WorldPos, expectedPitTime, sectionRecord.TotalDistance);
            IsPredictedReturnPositionFilled = sectionRecord.SessionTime != TimeSpan.Zero;
        }

        private ReturnPosition CreateReturnPosition(DriverTiming playerTiming, SectionRecord returnSector, DriverTiming driverInFront, DriverTiming driverBehind, bool isPositionInClass)
        {
            TimeSpan? gapBehind = null;
            TimeSpan? gapAhead = null;

            if (driverInFront != null)
            {
                gapAhead = playerTiming.DriverLapSectorsTracker.GetSectionRecord(driverInFront.DriverInfo.LapDistance).SessionTime - returnSector.SessionTime;
            }

            if (driverBehind != null)
            {
                gapBehind = returnSector.SessionTime - playerTiming.DriverLapSectorsTracker.GetSectionRecord(driverBehind.DriverInfo.LapDistance).SessionTime;
            }

            int? position = isPositionInClass ? driverInFront?.PositionInClass : driverInFront?.Position; 

            return new ReturnPosition(position ?? 1, driverInFront?.DriverInfo, driverBehind?.DriverInfo, gapAhead, gapBehind);
        }

        private PitPredictionReason IsPitStopRequested(SimulatorDataSet datasetToUse)
        {
            return datasetToUse.PlayerInfo.PitStopRequested ? PitPredictionReason.PitRequested : PitPredictionReason.None;
        }

        private bool IsDataSetValid(SimulatorDataSet dataSet)
        {
            return dataSet?.PlayerInfo != null && dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.SessionInfo.SessionPhase == SessionPhase.Green;
        }

        private PitPredictionReason IsInPitWindow(SimulatorDataSet simulatorDataSet, DriverTiming playerTiming)
        {
            if (simulatorDataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.InPitWindow || playerTiming.PitStops.Any(x => x.Completed && x.IsPitWindowStop))
            {
                return PitPredictionReason.None;
            }

            return PitPredictionReason.PitWindow;
        }

        private PitPredictionReason IsPitStopByTyre(SimulatorDataSet simulatorDataSet)
        {
            double wearBoundary = _settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel.TyreWear / 100.0;
            if (!simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.AllWheels.Any(x => x.TyreWear.ActualWear > wearBoundary))
            {
                return PitPredictionReason.None;
            }

            return PitPredictionReason.TyreStatus;
        }

        private PitPredictionReason IsPitStopByFuel(SimulatorDataSet simulatorDataSet)
        {
            double fuelLapsBoundary = _settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel.LapsRemaining;
            double fuelLapsRemaining = _fuelPredictionProvider.GetLapsLeft();

            if (fuelLapsRemaining < fuelLapsBoundary && _sessionRemainingCalculator.GetLapsRemaining(simulatorDataSet) > fuelLapsRemaining)
            {
                return PitPredictionReason.FuelLevel;
            }

            return PitPredictionReason.None;
        }

        private PitPredictionReason IsByStintEnding(SimulatorDataSet simulatorDataSet)
        {
            if (simulatorDataSet.PlayerInfo == null)
            {
                return PitPredictionReason.None;
            }

            return simulatorDataSet.PlayerInfo.StintTimeLeft != TimeSpan.Zero
                   && simulatorDataSet.PlayerInfo.StintTimeLeft.TotalMinutes < 8
                   && _sessionRemainingCalculator.GetTimeRemaining(simulatorDataSet) > simulatorDataSet.PlayerInfo.StintTimeLeft ? PitPredictionReason.StintEnding : PitPredictionReason.None;
        }
    }
}
namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.PitStop;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.PitBoard;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class PitStopDurationBoardProvider : AbstractPitBoardDataProvider, IDisposable
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IPitStopStatisticProvider _pitStopStatisticProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IDriverTimingProvider _driverTimingProvider;
        private readonly PitDurationBoardViewModel _pitDurationBoardViewModel;
        private readonly PitBoardSettingsViewModel _pitBoardSettingsViewModel;
        private bool _isPitBoardShown;
        private bool _isPlayerInPits;
        private TimeSpan _hideSessionTime;
        private Timer _refreshTimer;

        public PitStopDurationBoardProvider(ISettingsProvider settingsProvider, IPitStopStatisticProvider pitStopStatisticProvider, ISessionEventProvider sessionEventProvider,
            IDriverTimingProvider driverTimingProvider, IViewModelFactory viewModelFactory)
        {
            _settingsProvider = settingsProvider;
            _pitStopStatisticProvider = pitStopStatisticProvider;
            _sessionEventProvider = sessionEventProvider;
            _driverTimingProvider = driverTimingProvider;
            _sessionEventProvider.OnNewDataSet += SessionEventProviderOnOnNewDataSet;
            _pitDurationBoardViewModel = viewModelFactory.Create<PitDurationBoardViewModel>();
            _pitBoardSettingsViewModel = settingsProvider.DisplaySettingsViewModel.PitBoardSettingsViewModel;
        }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
        }

        public override void Reset()
        {
            _isPitBoardShown = false;
            _isPlayerInPits = false;
        }

        public void Dispose()
        {
            _sessionEventProvider.OnNewDataSet -= SessionEventProviderOnOnNewDataSet;
        }

        private void SessionEventProviderOnOnNewDataSet(object sender, DataSetArgs e)
        {
            if (!_pitBoardSettingsViewModel.IsPitDurationBoardEnabled)
            {
                return;
            }

            SimulatorDataSet dataSet = e.DataSet;
            if (dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.SessionInfo.SessionPhase != SessionPhase.Green ||
                dataSet.SessionInfo.SpectatingState != SpectatingState.Live)
            {
                Reset();
                return;
            }

            if (!_driverTimingProvider.TryGetPlayerTiming(out DriverTiming playerTiming) || playerTiming.DriverInfo.FinishStatus != DriverFinishStatus.None)
            {
                Reset();
                return;
            }

            if (_isPitBoardShown)
            {
                return;
            }

            if (playerTiming.InPits && !_isPlayerInPits && playerTiming.LastPitStop?.Completed == false)
            {
                UpdateViewModel(playerTiming.LastPitStop);
                _isPlayerInPits = true;
                _refreshTimer = new Timer(UpdateOnTimer, null, 50, 50);
            }
        }

        private void UpdateOnTimer(object state)
        {
            if (!_isPitBoardShown)
            {
                ShowBoard();
                return;
            }

            SimulatorDataSet dataSet = _sessionEventProvider.LastDataSet;
            if (dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.SessionInfo.SessionPhase != SessionPhase.Green ||
                dataSet.SessionInfo.SpectatingState != SpectatingState.Live)
            {
                HidePitBoard();
                return;
            }

            if (!_driverTimingProvider.TryGetPlayerTiming(out DriverTiming playerTiming) || playerTiming.DriverInfo.FinishStatus != DriverFinishStatus.None)
            {
                HidePitBoard();
                return;
            }

            if (!_isPlayerInPits && _hideSessionTime < dataSet.SessionInfo.SessionTime)
            {
                HidePitBoard();
                return;
            }

            if (playerTiming.LastPitStop != null && (playerTiming.InPits || (!_isPlayerInPits && _hideSessionTime >= dataSet.SessionInfo.SessionTime)))
            {
                UpdateViewModel(playerTiming.LastPitStop);
                return;
            }

            if (!playerTiming.InPits && _isPlayerInPits)
            {
                _hideSessionTime = dataSet.SessionInfo.SessionTime + TimeSpan.FromSeconds(10);
                _isPlayerInPits = false;
            }
        }

        private void HidePitBoard()
        {
            _isPitBoardShown = false;
            _refreshTimer.Dispose();
        }

        private void ShowBoard()
        {
            _hideSessionTime = TimeSpan.MaxValue;
            _isPitBoardShown = true;
            PitBoardController.RequestToShowPitBoard(_pitDurationBoardViewModel, 1, () => _isPitBoardShown);
        }

        private void UpdateViewModel(PitStopInfo pitStopInfo)
        {
            PitStatistics statistics = _pitStopStatisticProvider.GetCurrentStatisticsPlayerClass();

            if (pitStopInfo == null || statistics == null)
            {
                return;
            }

            if (statistics.PitLaneDuration != TimeSpan.Zero && statistics.PitStallDuration != TimeSpan.Zero && statistics.TotalPitDuration != TimeSpan.Zero)
            {
                _pitDurationBoardViewModel.AveragePitStallDuration = statistics.PitStallDuration.FormatTimeSpanOnlySecondNoMiliseconds(false);
                _pitDurationBoardViewModel.AveragePitStopDuration = statistics.TotalPitDuration.FormatTimeSpanOnlySecondNoMiliseconds(false);
                UpdatePitStopStatuses(pitStopInfo, statistics);
            }
            else
            {
                _pitDurationBoardViewModel.AveragePitStallDuration = string.Empty;
                _pitDurationBoardViewModel.AveragePitStopDuration = string.Empty;
            }

            _pitDurationBoardViewModel.PitStallDuration = pitStopInfo.PitStopStallDuration != TimeSpan.Zero ? pitStopInfo.PitStopStallDuration.FormatTimeSpanOnlySecondNoMiliseconds(false) : string.Empty;
            _pitDurationBoardViewModel.PitStopDuration = pitStopInfo.PitStopDuration != TimeSpan.Zero ? pitStopInfo.PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false) : string.Empty;
        }

        private void UpdatePitStopStatuses(PitStopInfo pitStopInfo, PitStatistics statistics)
        {
            if (pitStopInfo == null || statistics == null)
            {
                _pitDurationBoardViewModel.PitStallDurationStatus = PitInfoBriefDescriptionKind.None;
                _pitDurationBoardViewModel.PitStopDurationStatus = PitInfoBriefDescriptionKind.None;
                return;
            }

            _pitDurationBoardViewModel.PitStallDurationStatus = pitStopInfo.Phase is PitPhase.Exit or PitPhase.Completed
                ? PitTimeDifferenceToPitInfoBriefDescriptionKind(statistics.PitStallDuration - pitStopInfo.PitStopStallDuration) : PitInfoBriefDescriptionKind.None;

            _pitDurationBoardViewModel.PitStopDurationStatus = pitStopInfo.Phase is PitPhase.Completed
                ? PitTimeDifferenceToPitInfoBriefDescriptionKind(statistics.TotalPitDuration - pitStopInfo.PitStopDuration) : PitInfoBriefDescriptionKind.None;
        }

        private PitInfoBriefDescriptionKind PitTimeDifferenceToPitInfoBriefDescriptionKind(TimeSpan timeDifference)
        {
            return timeDifference.TotalSeconds switch
            {
                > 1 => PitInfoBriefDescriptionKind.PitStopSlowerThanPlayer,
                < -1 => PitInfoBriefDescriptionKind.PitStopFasterThanPlayer,
                _ => PitInfoBriefDescriptionKind.None
            };
        }
    }
}

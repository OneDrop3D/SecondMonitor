namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System.Collections.Generic;
    using System.Threading;
    using Common.SessionTiming.Drivers;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using ViewModels.PitBoard;
    using ViewModels.PitBoard.Controller;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;
    using ViewModels.StatusIcon;

    public class SafetyCarPitBoardProvider : AbstractPitBoardDataProvider
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private bool _isPitBoardShown;
        private Timer _refreshTimer;

        public SafetyCarPitBoardProvider(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider)
        {
            _settingsProvider = settingsProvider;
            _sessionEventProvider = sessionEventProvider;
            PitBoard = new SafetyCarPitBoardViewModel();
        }

        private SafetyCarPitBoardViewModel PitBoard { get; }

        public override void StartDataProvider(IPitBoardController pitBoardController)
        {
            base.StartDataProvider(pitBoardController);
            _sessionEventProvider.FlagStateChanged += SessionEventProviderOnFlagStateChanged;
        }

        private void SessionEventProviderOnFlagStateChanged(object sender, DataSetArgs e)
        {
            if (e.DataSet.SessionInfo.SpectatingState != SpectatingState.Live)
            {
                return;
            }

            if (!_settingsProvider.DisplaySettingsViewModel.PitBoardSettingsViewModel.IsYellowBoardEnabled)
            {
                return;
            }

            if (!_isPitBoardShown && IsSafetyCarCondition(e.DataSet))
            {
                _refreshTimer = new Timer(UpdateOnTimer, null, 50, 50);
            }
        }

        private void ShowBoard()
        {
            _isPitBoardShown = true;
            PitBoardController.RequestToShowPitBoard(PitBoard, 2, () => _isPitBoardShown);
        }

        private void UpdateOnTimer(object state)
        {
            if (!IsSafetyCarCondition(_sessionEventProvider.LastDataSet))
            {
                _isPitBoardShown = false;
                _refreshTimer.Dispose();
                return;
            }

            switch (_sessionEventProvider.LastDataSet.SessionInfo.SafetyCarState)
            {
                case SafetyCarState.WaitingForLeader:
                    PitBoard.AdditionalText = "Safety Car";
                    PitBoard.SafetyCarIconState = StatusIconState.Warning;
                    PitBoard.AdditionalTextState = StatusIconState.Warning;
                    break;
                case SafetyCarState.PitsClosed:
                    PitBoard.AdditionalText = "Pit Closed";
                    PitBoard.AdditionalTextState = StatusIconState.Error;
                    PitBoard.SafetyCarIconState = StatusIconState.Warning;
                    break;
                case SafetyCarState.PitsOpenForLeadLapCars:
                    PitBoard.AdditionalText = "Pit Open (Lead Lap Cars)";
                    PitBoard.AdditionalTextState = StatusIconState.Information;
                    PitBoard.SafetyCarIconState = StatusIconState.Warning;
                    break;
                case SafetyCarState.PitOpen:
                    PitBoard.AdditionalText = "Pit Open";
                    PitBoard.AdditionalTextState = StatusIconState.Information;
                    PitBoard.SafetyCarIconState = StatusIconState.Warning;
                    break;
                case SafetyCarState.LastScLap:
                    PitBoard.AdditionalText = "Ending";
                    PitBoard.AdditionalTextState = StatusIconState.Warning;
                    PitBoard.SafetyCarIconState = StatusIconState.Warning;
                    break;
                case SafetyCarState.LightsOff:
                    PitBoard.AdditionalText = "Ending";
                    PitBoard.AdditionalTextState = StatusIconState.Warning;
                    PitBoard.SafetyCarIconState = StatusIconState.Ok;
                    break;
                case SafetyCarState.None:
                default:
                    _isPitBoardShown = false;
                    _refreshTimer.Dispose();
                    return;
            }

            if (!_isPitBoardShown)
            {
                ShowBoard();
            }
        }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
        }

        public override void Reset()
        {
            _isPitBoardShown = false;
        }

        private static bool IsSafetyCarCondition(SimulatorDataSet dataSet)
        {
            return dataSet.SessionInfo.SafetyCarState != SafetyCarState.None;
        }
    }
}
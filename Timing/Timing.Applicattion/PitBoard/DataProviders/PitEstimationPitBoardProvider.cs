namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System;
    using System.Collections.Generic;

    using Common.SessionTiming.Drivers;

    using Contracts.Session;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.ViewModels.PitBoard;

    using ViewModels.SessionEvents;
    using ViewModels.Settings;

    public class PitEstimationPitBoardProvider : AbstractPitBoardDataProvider
    {
        private readonly IPlayerPitEstimationProvider _playerPitEstimationProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly ISettingsProvider _settingsProvider;
        private bool _isDriverInPits;

        public PitEstimationPitBoardProvider(IPlayerPitEstimationProvider playerPitEstimationProvider, ISessionEventProvider sessionEventProvider,
            ISettingsProvider settingsProvider)
        {
            _playerPitEstimationProvider = playerPitEstimationProvider;
            _sessionEventProvider = sessionEventProvider;
            _settingsProvider = settingsProvider;
            PitBoard = new PitReturnBoardViewModel();
        }

        private PitReturnBoardViewModel PitBoard { get; }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
            if (dataSet.SessionInfo.SessionType == SessionType.Race)
            {
                UpdateAndShowBoard();
            }
        }

        public override void OnNextData(SimulatorDataSet dataSet)
        {
            base.OnNextData(dataSet);

            if (dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.PlayerInfo is not { FinishStatus: DriverFinishStatus.None })
            {
                return;
            }

            if (_isDriverInPits == dataSet.PlayerInfo.InPits)
            {
                return;
            }

            if (!dataSet.PlayerInfo.InPits)
            {
                _isDriverInPits = false;
                return;
            }

            _isDriverInPits = true;

            if (!IsStarted || !_playerPitEstimationProvider.IsPredictedReturnPositionFilled ||
                !_settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel.ShowPitBoardAtEntry)
            {
                return;
            }

            ShowPitStopEstimation(2.0);
        }

        public override void Reset()
        {
            _playerPitEstimationProvider.Reset();
            _isDriverInPits = false;
        }

        private void UpdateAndShowBoard()
        {
            if (!IsStarted ||
                !_playerPitEstimationProvider.IsPlayerExpectedToPit ||
                !_playerPitEstimationProvider.IsPredictedReturnPositionFilled ||
                !_settingsProvider.DisplaySettingsViewModel.PitEstimationSettingsViewModel.IsReasonEnabled(_playerPitEstimationProvider.PitPredictionReason,
                    x => x.IsPitBoardEnabled))
            {
                return;
            }

            ShowPitStopEstimation(1.0);
        }

        private void ShowPitStopEstimation(double displayTimeScale)
        {
            PitReturnPrediction currentEstimation = _playerPitEstimationProvider.CurrentPitReturnPrediction;
            if (currentEstimation.PitTime == TimeSpan.Zero)
            {
                return;
            }

            PitBoard.ReturnPosition = FormatPositionInClass(currentEstimation.OverallReturnPosition.Position, currentEstimation.ClassReturnPosition.Position);
            PitBoard.PitTime = currentEstimation.PitTime.FormatTimeSpanOnlySecondNoMiliseconds(false) + "s";
            PitBoard.FromModel(currentEstimation.ClassReturnPosition);
            PitBoardController.RequestToShowPitBoard(PitBoard, 1,
                TimeSpan.FromSeconds(_settingsProvider.DisplaySettingsViewModel.PitBoardSettingsViewModel.DisplaySeconds * displayTimeScale));
        }

        private string FormatPositionInClass(int overallPosition, int positionInClass)
        {
            if (_sessionEventProvider.LastDataSet?.SessionInfo?.IsMultiClass == false || positionInClass <= 0)
            {
                return overallPosition.ToString();
            }

            switch (_settingsProvider.DisplaySettingsViewModel.MultiClassDisplayKind)
            {
                case MultiClassDisplayKind.OnlyOverall:
                    return overallPosition.ToString();
                case MultiClassDisplayKind.OnlyClass:
                    return positionInClass.ToString();
                case MultiClassDisplayKind.ClassFirst:
                    return $"{positionInClass.ToString()} ({overallPosition.ToString()})";
                case MultiClassDisplayKind.OverallFirst:
                    return $"{overallPosition.ToString()} ({positionInClass.ToString()})";
                default:
                    return overallPosition.ToString();
            }
        }
    }
}

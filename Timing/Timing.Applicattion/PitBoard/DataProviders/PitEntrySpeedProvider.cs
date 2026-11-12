namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System;
    using System.Collections.Generic;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.PitBoard;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class PitEntrySpeedProvider : AbstractPitBoardDataProvider, IDisposable
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private readonly PitEntrySpeedBoardViewModel _viewModel;
        private bool _isDriverInPits;

        public PitEntrySpeedProvider(ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider, IViewModelFactory viewModelFactory)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _sessionEventProvider = sessionEventProvider;
            _sessionEventProvider.OnNewDataSet += SessionEventProviderOnOnNewDataSet;
            _viewModel = viewModelFactory.Create<PitEntrySpeedBoardViewModel>();
        }

        private void SessionEventProviderOnOnNewDataSet(object sender, DataSetArgs e)
        {
            SimulatorDataSet dataSet = e.DataSet;

            if (dataSet.PlayerInfo == null || !_displaySettingsViewModel.PitBoardSettingsViewModel.IsPitEntrySpeedBoardEnabled)
            {
                return;
            }

            if (_isDriverInPits && !dataSet.PlayerInfo.InPits)
            {
                _isDriverInPits = false;
                return;
            }

            if (!dataSet.PlayerInfo.InPits || _isDriverInPits)
            {
                return;
            }

            _isDriverInPits = true;
            if (dataSet.PlayerInfo.Speed.InKph > 10)
            {
                ShowPitBoard(dataSet.PlayerInfo);
            }
        }

        private void ShowPitBoard(DriverInfo playerInfo)
        {
            _viewModel.SpeedFormatted =
                $"{playerInfo.Speed.GetValueInUnits(_displaySettingsViewModel.VelocityUnits):F1} {Velocity.GetUnitSymbol(_displaySettingsViewModel.VelocityUnits)}";
            PitBoardController.RequestToShowPitBoard(_viewModel, 1, TimeSpan.FromSeconds(_displaySettingsViewModel.PitBoardSettingsViewModel.DisplaySeconds));
        }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
        }

        public override void Reset()
        {
            _isDriverInPits = false;
        }

        public void Dispose()
        {
            _sessionEventProvider.OnNewDataSet -= SessionEventProviderOnOnNewDataSet;
        }
    }
}

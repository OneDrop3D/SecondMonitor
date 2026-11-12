namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Common.SessionTiming.Drivers;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using ViewModels.Factory;
    using ViewModels.PitBoard;
    using ViewModels.PitBoard.Controller;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;

    public class ClearToRejoinBoardProvider : AbstractPitBoardDataProvider
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private bool _isPitBoardShown;
        private Timer _refreshTimer;
        private DateTime _hideSystemTime;

        public ClearToRejoinBoardProvider(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider, IViewModelFactory viewModelFactory)
        {
            _settingsProvider = settingsProvider;
            _sessionEventProvider = sessionEventProvider;
            PitBoard = viewModelFactory.Create<ClearToRejoinBoardViewModel>();
            _hideSystemTime = DateTime.MaxValue;
        }

        private ClearToRejoinBoardViewModel PitBoard { get; }

        public override void StartDataProvider(IPitBoardController pitBoardController)
        {
            base.StartDataProvider(pitBoardController);
            _sessionEventProvider.FlagStateChanged += SessionEventProviderOnFlagStateChanged;
            /*PitBoardController.RequestToShowPitBoard(PitBoard, 3, TimeSpan.FromSeconds(20));*/
        }

        private void SessionEventProviderOnFlagStateChanged(object sender, DataSetArgs e)
        {
            if (!_settingsProvider.DisplaySettingsViewModel.PitBoardSettingsViewModel.IsYellowBoardEnabled || e.DataSet.PlayerInfo == null)
            {
                return;
            }

            if (_isPitBoardShown && !e.DataSet.PlayerInfo.IsCausingYellow)
            {
                _hideSystemTime = DateTime.Now.AddSeconds(3);
                return;
            }

            if (!_isPitBoardShown && !e.DataSet.PlayerInfo.IsCausingYellow)
            {
                return;
            }

            if (e.DataSet.PlayerInfo.IsCausingYellow)
            {
                var driversAffectedOrdered = GetDriversAffectedByRejoinOrdered(e.DataSet);
                ShowBoard(driversAffectedOrdered);
            }
        }

        private void ShowBoard(List<DriverInfo> driversAffectedByDriver)
        {
            _isPitBoardShown = true;
            _refreshTimer?.Dispose();
            _hideSystemTime = DateTime.MaxValue;
            UpdateViewModel(driversAffectedByDriver);
            PitBoardController.RequestToShowPitBoard(PitBoard, 3, () => _isPitBoardShown);
            _refreshTimer = new Timer(UpdateOnTimer, null, 50, 50);
        }

        private void UpdateOnTimer(object state)
        {
            var dataSet = _sessionEventProvider.LastDataSet;
            if (!_isPitBoardShown)
            {
                _refreshTimer.Dispose();
                return;
            }

            if (dataSet.PlayerInfo == null)
            {
                return;
            }

            if (_hideSystemTime < DateTime.Now)
            {
                _isPitBoardShown = false;
                return;
            }

            if (!dataSet.PlayerInfo.IsCausingYellow && _hideSystemTime == DateTime.MaxValue)
            {
                _hideSystemTime = DateTime.Now.AddSeconds(3);
                return;
            }

            if (_isPitBoardShown)
            {
                var affectedDriversOrdered = GetDriversAffectedByRejoinOrdered(dataSet);
                UpdateViewModel(affectedDriversOrdered);
            }
            else
            {
                _refreshTimer.Dispose();
            }
        }

        private List<DriverInfo> GetDriversAffectedByRejoinOrdered(SimulatorDataSet dataSet)
        {
            return dataSet.DriversInfo.Where(x => !x.IsPlayer && !x.InPits && x.FinishStatus == DriverFinishStatus.None && x.DistanceToPlayer > 0 && x.DistanceToPlayer < 500).OrderBy(x => x.DistanceToPlayer).ToList();
        }

        private void UpdateViewModel(List<DriverInfo> driversAffectedByRejoin)
        {
            PitBoard.FromModel(driversAffectedByRejoin);
        }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
        }

        public override void Reset()
        {
            _isPitBoardShown = false;
            _hideSystemTime = DateTime.MaxValue;
        }
    }
}
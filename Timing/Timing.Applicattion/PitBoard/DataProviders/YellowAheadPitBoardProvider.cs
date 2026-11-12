namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Windows;

    using Common.SessionTiming.Drivers;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;

    using SecondMonitor.ViewModels.Factory;

    using ViewModels.PitBoard;
    using ViewModels.PitBoard.Controller;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;

    public class YellowAheadPitBoardProvider : AbstractPitBoardDataProvider
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly object _lockObject = new object();
        private bool _isPitBoardShown;
        private Timer _refreshTimer;

        public YellowAheadPitBoardProvider(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider, IViewModelFactory viewModelFactory)
        {
            _settingsProvider = settingsProvider;
            _sessionEventProvider = sessionEventProvider;
            PitBoard = viewModelFactory.Create<YellowAheadPitBoardViewModel>();
        }

        private YellowAheadPitBoardViewModel PitBoard { get; }

        public override void StartDataProvider(IPitBoardController pitBoardController)
        {
            base.StartDataProvider(pitBoardController);
            _sessionEventProvider.FlagStateChanged += SessionEventProviderOnFlagStateChanged;
        }

        private void SessionEventProviderOnFlagStateChanged(object sender, DataSetArgs e)
        {
            lock (_lockObject)
            {
                if (e.DataSet.SessionInfo.SpectatingState != SpectatingState.Live)
                {
                    return;
                }

                if (!_settingsProvider.DisplaySettingsViewModel.PitBoardSettingsViewModel.IsYellowBoardEnabled)
                {
                    return;
                }

                if (!_isPitBoardShown && IsYellowFlagCondition(e.DataSet))
                {
                    _refreshTimer = new Timer(UpdateOnTimer, null, 50, 50);
                }
            }
        }

        private void ShowBoard()
        {
            _isPitBoardShown = true;
            PitBoardController.RequestToShowPitBoard(PitBoard, 2, () => _isPitBoardShown);
        }

        private void UpdateOnTimer(object state)
        {
            lock (_lockObject)
            {
                if (!IsYellowFlagCondition(_sessionEventProvider.LastDataSet))
                {
                    _isPitBoardShown = false;
                    _refreshTimer.Dispose();
                    return;
                }

                SimulatorDataSet lastDataSet = _sessionEventProvider.LastDataSet;
                List<DriverInfo> driversCausingYellow = GetDriversCausingYellow(lastDataSet);
                if (driversCausingYellow.Count > 0 && driversCausingYellow[0].DistanceToPlayer is < 0 and > -800)
                {
                    UpdateViewModel(driversCausingYellow[0], lastDataSet.SessionInfo.IsMultiClass);
                }
                else
                {
                    UpdateViewModelWithSectorInformation(driversCausingYellow, lastDataSet.SessionInfo.IsMultiClass);
                }

                if (!_isPitBoardShown)
                {
                    ShowBoard();
                }
            }
        }

        private List<DriverInfo> GetDriversCausingYellow(SimulatorDataSet dataSet)
        {
            return dataSet.DriversInfo.Where(x => !x.IsPlayer && x.IsCausingYellow).OrderByDescending(x => x.DistanceToPlayer).ToList();
        }

        private bool IsYellowFlagCondition(SimulatorDataSet dataSet)
        {
            return !dataSet.PlayerInfo.InPits && dataSet.SessionInfo.IsYellowFlagSituation;
        }

        private void UpdateViewModel(DriverInfo causingYellowDriver, bool isMulticlass)
        {
            PitBoard.SetClosestDriver(new DriverWithClassModel(causingYellowDriver.PositionInClass, causingYellowDriver.DriverShortName, causingYellowDriver.CarClassId, isMulticlass),
                Distance.FromMeters(-causingYellowDriver.DistanceToPlayer).GetByUnit(_settingsProvider.DisplaySettingsViewModel.DistanceUnitsSmall).ToString("N0") + Distance.GetUnitsSymbol(_settingsProvider.DisplaySettingsViewModel.DistanceUnitsSmall));
        }

        private void UpdateViewModelWithSectorInformation(List<DriverInfo> driversCausingYellow, bool isMulticlass)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateViewModelWithSectorInformation(driversCausingYellow, isMulticlass));
                return;
            }

            StringBuilder sb = new StringBuilder();
            FlagKind flags = _sessionEventProvider.LastDataSet.SessionInfo.ActiveFlags;
            if (flags.HasFlag(FlagKind.YellowSector1))
            {
                sb.Append("Sector 1");
            }

            if (flags.HasFlag(FlagKind.YellowSector2))
            {
                sb.Append(sb.Length > 0 ? "\nSector 2" : "Sector 2");
            }

            if (flags.HasFlag(FlagKind.YellowSector3))
            {
                sb.Append(sb.Length > 0 ? "\nSector 3" : "Sector 3");
            }

            if (driversCausingYellow.Count > 0)
            {
                double halfTrackDistance = _sessionEventProvider.LastDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters / 2.0;
                bool isDriverAhead = driversCausingYellow[0].DistanceToPlayer > -halfTrackDistance && driversCausingYellow[0].DistanceToPlayer < 0;
                PitBoard.IsSectorAhead = isDriverAhead;
                PitBoard.IsSectorAheadArrowShown = true;
            }
            else
            {
                PitBoard.IsSectorAheadArrowShown = false;
            }

            PitBoard.SetSectorInformation(sb.ToString(),
                driversCausingYellow
                    .Take(3)
                    .Select(x => new DriverWithClassModel(x.PositionInClass, x.DriverShortName, x.CarClassId, isMulticlass)).ToList());
        }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
        }

        public override void Reset()
        {
            lock (_lockObject)
            {
                _isPitBoardShown = false;
            }
        }
    }
}
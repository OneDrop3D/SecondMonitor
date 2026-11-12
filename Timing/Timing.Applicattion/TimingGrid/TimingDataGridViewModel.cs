namespace SecondMonitor.Timing.Application.TimingGrid
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Columns;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Ordering;
    using Contracts.Session;
    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.DriversPresentation;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap;

    using SessionTiming.Drivers.Presentation.ViewModel;
    using SimdataManagement.DriverPresentation;
    using ViewModel;
    using ViewModels;
    using ViewModels.Colors;
    using ViewModels.DataGrid;
    using ViewModels.Factory;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;
    using ViewModels.Settings.Model;
    using ViewModels.Settings.ViewModel;

    public class TimingDataGridViewModel : AbstractViewModel, ISessionInformationProvider
    {
        public const string ViewModelLayoutName = "Timing Data Grid";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly DriverPresentationsManager _driverPresentationsManager;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private readonly IClassColorProvider _classColorProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly TimingGridColumnsFactory _timingGridColumnsFactory;
        private readonly ViewModelFactory _viewModelFactory;
        private readonly DriversOrderingFactory _driversOrderingFactory;
        private readonly ColumnVisibilityAdapterFactory _columnVisibilityAdapterFactory;
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, DriverTiming> _driverNameTimingMap;
        private readonly Stopwatch _refreshGapWatch;
        private readonly Dictionary<string, List<IColumnVisibilityAdapter>> _columnsVisibilityAdapters;
        private readonly Stopwatch _refreshColumnVisibilityWatch;

        private int _loadIndex;
        private int _currentRefreshCarIndex;
        private SessionOptionsViewModel _currentSessionOptionsViewModel;
        private ICommand _openSelectedDriversLapsCommand;
        private DriverTimingViewModel _selectedDriverTimingViewModel;
        private ObservableCollection<ColumnDescriptorViewModel> _columns;
        private bool _highlightPLayer;
        private int _highlightPlayerFontSize;
        private IDriversOrdering _driversOrdering;
        private DriverTiming _previouslyRefreshedDriver;

        public TimingDataGridViewModel(DriverPresentationsManager driverPresentationsManager, ISettingsProvider settingsProvider, IClassColorProvider classColorProvider,
            SessionInfoViaTimingViewModelProvider sessionInfoViaTimingViewModelProvider, ISessionEventProvider sessionEventProvider, TimingGridColumnsFactory timingGridColumnsFactory, ViewModelFactory viewModelFactory,
            DriversOrderingFactory driversOrderingFactory, ColumnVisibilityAdapterFactory columnVisibilityAdapterFactory)
        {
            _refreshColumnVisibilityWatch = Stopwatch.StartNew();
            _refreshGapWatch = Stopwatch.StartNew();
            _columnsVisibilityAdapters = new Dictionary<string, List<IColumnVisibilityAdapter>>();
            _loadIndex = 0;
            _driverNameTimingMap = new Dictionary<string, DriverTiming>();
            _driverPresentationsManager = driverPresentationsManager;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;

            _classColorProvider = classColorProvider;
            _sessionEventProvider = sessionEventProvider;
            _timingGridColumnsFactory = timingGridColumnsFactory;
            _viewModelFactory = viewModelFactory;
            _driversOrderingFactory = driversOrderingFactory;
            _columnVisibilityAdapterFactory = columnVisibilityAdapterFactory;
            sessionInfoViaTimingViewModelProvider.RegisterRelay(this);
            DriversViewModels = new ObservableCollection<DriverTimingViewModel>();
            _driverPresentationsManager.DriverCustomColorChanged += DriverPresentationsManagerOnDriverCustomColorEnabledChanged;
            CurrentSessionOptionsViewModel = _displaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView;
            sessionEventProvider.SessionTypeChange += SessionEventProviderOnSessionTypeChange;
            _columns = new ObservableCollection<ColumnDescriptorViewModel>();
            _displaySettingsViewModel.SessionOptionsViewModel.ColumnDescriptorsChanged += SessionOptionsViewModelOnColumnDescriptorsChanged;
            settingsProvider.DisplaySettingsPropertyChanged += SettingsProviderOnDisplaySettingsPropertyChanged;
        }

        private SessionOptionsViewModel CurrentSessionOptionsViewModel
        {
            get => _currentSessionOptionsViewModel;
            set => SetProperty(ref _currentSessionOptionsViewModel, value);
        }

        public ObservableCollection<ColumnDescriptorViewModel> Columns
        {
            get => _columns;
            set => SetProperty(ref _columns, value);
        }

        public bool HighlightPLayer
        {
            get => _highlightPLayer;
            set => SetProperty(ref _highlightPLayer, value);
        }

        public int HighlightPlayerFontSize
        {
            get => _highlightPlayerFontSize;
            set => SetProperty(ref _highlightPlayerFontSize, value);
        }

        public ICommand OpenSelectedDriversLapsCommand
        {
            get => _openSelectedDriversLapsCommand;
            set => SetProperty(ref _openSelectedDriversLapsCommand, value);
        }

        public ObservableCollection<DriverTimingViewModel> DriversViewModels { get; }

        public DriverTimingViewModel SelectedDriverTimingViewModel
        {
            get => _selectedDriverTimingViewModel;
            set => SetProperty(ref _selectedDriverTimingViewModel, value);
        }

        public bool IsScrollToPlayerEnabled => _displaySettingsViewModel.ScrollToPlayer;

        public void UpdateProperties(SimulatorDataSet dataSet)
        {
            if (_loadIndex > 0 || dataSet == null || dataSet.SessionInfo.SpectatingState == SpectatingState.Replay)
            {
                return;
            }

            int driversUpdatedPerTick = _displaySettingsViewModel.DriversUpdatedPerTick;
            List<DriverTiming> nonActiveDrivers;
            lock (_lockObject)
            {
                string selectedOrdering = (dataSet.SessionInfo.SessionPhase == SessionPhase.Countdown ? DriverOrderKind.Absolute : CurrentSessionOptionsViewModel.OrderingMode).ToString();
                if (_driversOrdering == null || selectedOrdering != _driversOrdering.Name)
                {
                    _driversOrdering = _driversOrderingFactory.Create(selectedOrdering);
                    _driverNameTimingMap.Values.ForEach(x => x.DriverTimingVisualization.Reset());
                }

                List<DriverTiming> orderedTimings = _driversOrdering.Order(_driverNameTimingMap.Values);
                //List<DriverTiming> absolutelyOrderedDrivers = orderedTimings.OrderBy(x => x.Position).ToList();

                if (DriversViewModels.Count != _driverNameTimingMap.Count)
                {
                    Logger.Error("Viewmodels and id map count missmatch");
                    Application.Current.Dispatcher.InvokeAsync(() => MatchDriversList(orderedTimings));
                    return;
                }

                for (int i = _currentRefreshCarIndex; i < orderedTimings.Count && i - _currentRefreshCarIndex < driversUpdatedPerTick; i++)
                {
                    if (i == 0 && DriversViewModels[i].DriverTiming.Position == 1)
                    {
                        _previouslyRefreshedDriver = null;
                    }

                    RebindViewModel(DriversViewModels[i], orderedTimings[i]);
                    DriversViewModels[i].RefreshProperties(dataSet, _previouslyRefreshedDriver);
                    _previouslyRefreshedDriver = DriversViewModels[i].DriverTiming;
                }

                _currentRefreshCarIndex += driversUpdatedPerTick;

                if (_currentRefreshCarIndex >= dataSet.DriversInfo.Length)
                {
                    _currentRefreshCarIndex = 0;
                }

                if (_refreshGapWatch.ElapsedMilliseconds > 500)
                {
                    UpdateGapsSize(dataSet);
                    _refreshGapWatch.Restart();
                }

                nonActiveDrivers = DriversViewModels.Where(x => !x.DriverTiming.IsActive).Select(x => x.DriverTiming).ToList();
            }

            RemoveDrivers(nonActiveDrivers);

            if (_refreshColumnVisibilityWatch.Elapsed.TotalSeconds > 5)
            {
                RefreshColumnVisibility();
                _refreshColumnVisibilityWatch.Restart();
            }
        }

        private void RebindViewModel(DriverTimingViewModel driverTimingViewModel, DriverTiming driverTiming)
        {
            if (driverTimingViewModel.DriverTiming == driverTiming)
            {
                return;
            }

            if (driverTimingViewModel.DriverTiming.CarClassId != driverTiming.CarClassId)
            {
                driverTimingViewModel.ClassIndicationBrush = _classColorProvider.GetColorForClass(driverTiming.CarClassId);
            }

            driverTimingViewModel.OutLineColor = GetDriverOutline(driverTiming.DriverInfo);
            driverTimingViewModel.DriverTiming = driverTiming;
        }

        private ColorDto GetDriverOutline(DriverInfo driverInfo)
        {
            _driverPresentationsManager.TryGetDriverPresentation(driverInfo.DriverLongName, out DriverPresentationDto driverPresentationDto);
            return driverPresentationDto?.CustomOutLineEnabled == true ? driverPresentationDto.OutLineColor : null;
        }

        private void UpdateGapsSize(SimulatorDataSet dataSet)
        {
            if (dataSet?.SessionInfo?.SessionType != SessionType.Race || CurrentSessionOptionsViewModel.OrderingMode != DriverOrderKind.Relative)
            {
                return;
            }

            bool isVisualizationEnabled = _displaySettingsViewModel.IsGapVisualizationEnabled;
            double minimalGap = _displaySettingsViewModel.MinimalGapForVisualization;
            double heightPerSecond = _displaySettingsViewModel.GapHeightForOneSecond;
            double maximumGap = _displaySettingsViewModel.MaximumGapHeight;

            DriverTimingViewModel previousViewModel = null;
            foreach (DriverTimingViewModel driverTimingViewModel in DriversViewModels)
            {
                if (previousViewModel == null)
                {
                    previousViewModel = driverTimingViewModel;
                    continue;
                }

                /*if ((previousViewModel.IsLapping && driverTimingViewModel.IsLapping) || (driverTimingViewModel.IsLapped &&  driverTimingViewModel.IsLapped))
                {
                    previousViewModel.GapHeight = 0;
                    previousViewModel = driverTimingViewModel;
                    continue;
                }*/

                double gapInSeconds = Math.Abs(driverTimingViewModel.GapToPlayer.TotalSeconds - previousViewModel.GapToPlayer.TotalSeconds);
                double gapInSecondsWithMinimal = gapInSeconds - minimalGap;
                if (!isVisualizationEnabled || gapInSecondsWithMinimal < 0)
                {
                    previousViewModel.GapHeight = 0;
                    previousViewModel = driverTimingViewModel;
                    continue;
                }

                previousViewModel.GapToNextDriver = gapInSeconds;
                previousViewModel.GapHeight = Math.Round(Math.Min(gapInSecondsWithMinimal * heightPerSecond, maximumGap));
                previousViewModel = driverTimingViewModel;
            }
        }

        private void RemoveDrivers(IEnumerable<DriverTiming> drivers)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RemoveDrivers(drivers));
                return;
            }

            lock (_lockObject)
            {
                drivers.ForEach(RemoveDriver);
            }
        }

        public void RemoveDriver(DriverTiming driver)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RemoveDriver(driver));
                return;
            }

            lock (_lockObject)
            {
                DriverTimingViewModel toRemove = DriversViewModels.FirstOrDefault(x => x.DriverTiming == driver);
                if (toRemove == null)
                {
                    return;
                }

                if (_driverNameTimingMap.ContainsKey(driver.DriverId))
                {
                    Logger.Warn($"Driver Id {driver.DriverId} not found");
                    _driverNameTimingMap.Remove(driver.DriverId);
                }

                Logger.Info($"Removing driver {toRemove.DriverId}");
                if (DriversViewModels.Remove(toRemove))
                {
                    Logger.Info("Driver Removed");
                }
            }
        }

        public void AddDriver(DriverTiming driver)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => AddDriver(driver));
                return;
            }

            DriverTimingViewModel newViewModel = _viewModelFactory.Set<DriverTiming>().To(driver).Create<DriverTimingViewModel>();
            lock (_lockObject)
            {
                Logger.Info($"Adding driver {driver.DriverId}");
                //If possible, rebind - do not create new
                if (_driverNameTimingMap.ContainsKey(driver.DriverId))
                {
                    _driverNameTimingMap[driver.DriverId] = driver;
                    //If Viewmodel with this driver not found, then rebind any viewmodel
                    var viewModelToRebind = DriversViewModels.FirstOrDefault(x => x.DriverId == driver.DriverId) ?? DriversViewModels.First();
                    RebindViewModel(viewModelToRebind, driver);
                    return;
                }

                _driverNameTimingMap[driver.DriverId] = driver;
                newViewModel.OutLineColor = GetDriverOutline(driver.DriverInfo);
                newViewModel.ClassIndicationBrush = GetClassColor(driver.DriverInfo);
                DriversViewModels.Add(newViewModel);
            }
        }

        public void MatchDriversList(List<DriverTiming> drivers)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => MatchDriversList(drivers));
                return;
            }

            Logger.Trace("Match Driver List Start");

            lock (_lockObject)
            {
                _previouslyRefreshedDriver = null;
                int driversCountToRemap = Math.Min(drivers.Count, DriversViewModels.Count);
                Logger.Info("Drivers to Remap: " + driversCountToRemap);
                int currentDriverIndex;
                _driverNameTimingMap.Clear();
                for (currentDriverIndex = 0; currentDriverIndex < driversCountToRemap; currentDriverIndex++)
                {
                    DriverTiming currentDriver = drivers[currentDriverIndex];
                    Logger.Info($"Remaping drivers, old driver = {DriversViewModels[currentDriverIndex].DriverId} new driver = {currentDriver.DriverId}");
                    RebindViewModel(DriversViewModels[currentDriverIndex], currentDriver);
                    _driverNameTimingMap[currentDriver.DriverId] = currentDriver;
                }

                if (DriversViewModels.Count < drivers.Count)
                {
                    for (int i = currentDriverIndex; i < drivers.Count; i++)
                    {
                        AddDriver(drivers[i]);
                    }
                }

                if (DriversViewModels.Count > drivers.Count)
                {
                    while (DriversViewModels.Count > drivers.Count)
                    {
                        Logger.Info($"Removing driver {DriversViewModels[DriversViewModels.Count - 1].DriverId}");
                        DriversViewModels.RemoveAt(DriversViewModels.Count - 1);
                    }
                }

                DriversViewModels.ForEach(x => x.GapHeight = 0);
            }

            Logger.Trace("Match Driver List End");
        }

        public bool IsDriverOnValidLap(IDriverInfo driver)
        {
            if (string.IsNullOrEmpty(driver?.DriverSessionId))
            {
                return false;
            }

            lock (_lockObject)
            {
                if (_driverNameTimingMap.TryGetValue(driver.DriverSessionId, out DriverTiming timing))
                {
                    return timing.CurrentLap?.Valid ?? false;
                }
            }

            return false;
        }

        public bool IsDriverLastSectorGreen(IDriverInfo driver, int sectorNumber)
        {
            if (string.IsNullOrEmpty(driver?.DriverSessionId))
            {
                return false;
            }

            DriverTiming timing;
            lock (_lockObject)
            {
                if (!_driverNameTimingMap.TryGetValue(driver.DriverSessionId, out timing))
                {
                    return false;
                }
            }

            switch (sectorNumber)
            {
                case 1:
                    return timing.IsLastSector1PersonalBest;
                case 2:
                    return timing.IsLastSector2PersonalBest;
                case 3:
                    return timing.IsLastSector3PersonalBest;
                default:
                    return false;
            }
        }

        public bool IsDriverLastSectorPurple(IDriverInfo driver, int sectorNumber)
        {
            if (string.IsNullOrEmpty(driver?.DriverSessionId))
            {
                return false;
            }

            DriverTiming timing;
            lock (_lockObject)
            {
                if (!_driverNameTimingMap.TryGetValue(driver.DriverSessionId, out timing))
                {
                    return false;
                }
            }

            switch (sectorNumber)
            {
                case 1:
                    return timing.IsLastSector1SessionBest;
                case 2:
                    return timing.IsLastSector2SessionBest;
                case 3:
                    return timing.IsLastSector3SessionBest;
                default:
                    return false;
            }
        }

        public bool IsDriverLastClassSectorPurple(IDriverInfo driver, int sectorNumber)
        {
            if (string.IsNullOrEmpty(driver?.DriverSessionId))
            {
                return false;
            }

            DriverTiming timing;
            lock (_lockObject)
            {
                if (!_driverNameTimingMap.TryGetValue(driver.DriverSessionId, out timing))
                {
                    return false;
                }
            }

            switch (sectorNumber)
            {
                case 1:
                    return timing.IsLastSector1ClassSessionBest;
                case 2:
                    return timing.IsLastSector2ClassSessionBest;
                case 3:
                    return timing.IsLastSector3ClassSessionBest;
                default:
                    return false;
            }
        }

        public bool HasDriverCompletedPitWindowStop(IDriverInfo driver)
        {
            if (string.IsNullOrEmpty(driver?.DriverSessionId))
            {
                return false;
            }

            lock (_lockObject)
            {
                if (_driverNameTimingMap.TryGetValue(driver.DriverSessionId, out DriverTiming timing))
                {
                    return timing.PitStops.Any(x => x.IsPitWindowStop && !x.WasDriveThrough);
                }
            }

            return false;
        }

        public int? GetPlayerPitStopCount()
        {
            return _driverNameTimingMap.Values.FirstOrDefault(x => x.IsPlayer)?.PitCount;
        }

        public int GetDriverPitStopCount(IDriverInfo driver)
        {
            if (string.IsNullOrEmpty(driver?.DriverSessionId))
            {
                return 0;
            }

            lock (_lockObject)
            {
                if (_driverNameTimingMap.TryGetValue(driver.DriverSessionId, out DriverTiming timing))
                {
                    return timing.PitCount;
                }
            }

            return 0;
        }

        private void SetSessionOptionOfCurrentSession(SimulatorDataSet dataSet)
        {
            if (dataSet == null)
            {
                return;
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => SetSessionOptionOfCurrentSession(dataSet));
                return;
            }

            List<ColumnDescriptor> columns = _timingGridColumnsFactory.CreateColumns(dataSet.SessionInfo.SessionType).ToList();
            if (columns.Count == 0)
            {
                return;
            }

            CreateColumnVisibilityAdapters(columns);
            _columns.Clear();
            columns.ForEach(x =>
            {
                var columnDescriptorViewModel = new ColumnDescriptorViewModel();
                columnDescriptorViewModel.FromModel(x);
                _columns.Add(columnDescriptorViewModel);
            });

            switch (dataSet.SessionInfo.SessionType)
            {
                case SessionType.Practice:
                case SessionType.WarmUp:
                    CurrentSessionOptionsViewModel = _displaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView;
                    break;
                case SessionType.Qualification:
                    CurrentSessionOptionsViewModel = _displaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView;
                    break;
                case SessionType.Race:
                    CurrentSessionOptionsViewModel = _displaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView;
                    break;
                default:
                    CurrentSessionOptionsViewModel = _displaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView;
                    break;
            }

            UpdatePropertiesForCurrentSessionType();
        }

        public ColorDto GetClassColor(IDriverInfo driverInfo)
        {
            return _classColorProvider.GetColorForClass(driverInfo.CarClassId);
        }

        private void CreateColumnVisibilityAdapters(IEnumerable<ColumnDescriptor> columns)
        {
            _columnsVisibilityAdapters.Clear();
            foreach (ColumnDescriptor columnDescriptor in columns.Where(x => x.IsAutoHideCapable && x.IsAutoHideEnabled))
            {
                if (_columnsVisibilityAdapters.ContainsKey(columnDescriptor.Name))
                {
                    continue;
                }

                List<IColumnVisibilityAdapter> columnVisibilityAdapters = _columnVisibilityAdapterFactory.CreateAll(columnDescriptor.Name).ToList();
                if (columnVisibilityAdapters.Count == 0)
                {
                    continue;
                }

                _columnsVisibilityAdapters.Add(columnDescriptor.Name, columnVisibilityAdapters);
            }
        }

        private void UpdatePropertiesForCurrentSessionType()
        {
            HighlightPLayer = CurrentSessionOptionsViewModel.HighlightPlayer;
            HighlightPlayerFontSize = CurrentSessionOptionsViewModel.HighlightFontSize;
        }

        private void DriverPresentationsManagerOnDriverCustomColorEnabledChanged(object sender, DriverCustomColorEnabledArgs e)
        {
            _driverPresentationsManager.TryGetOutLineColor(e.DriverLongName, out ColorDto colorDto);
            DriversViewModels.Where(x => x.DriverLongName == e.DriverLongName).ForEach(x => x.OutLineColor = colorDto);
        }

        private void SessionEventProviderOnSessionTypeChange(object sender, DataSetArgs e)
        {
            SetSessionOptionOfCurrentSession(e.DataSet);
        }

        private void SessionOptionsViewModelOnColumnDescriptorsChanged(object sender, EventArgs e)
        {
            SetSessionOptionOfCurrentSession(_sessionEventProvider.LastDataSet);
        }

        private void SettingsProviderOnDisplaySettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdatePropertiesForCurrentSessionType();
        }

        private void RefreshColumnVisibility()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(RefreshColumnVisibility);
                return;
            }

            foreach (ColumnDescriptorViewModel columnDescriptorViewModel in Columns.Where(x => x.ColumnDescriptor.IsAutoHideEnabled))
            {
                if (!_columnsVisibilityAdapters.TryGetValue(columnDescriptorViewModel.ColumnDescriptor.Name, out List<IColumnVisibilityAdapter> visibilityAdapters))
                {
                    continue;
                }

                columnDescriptorViewModel.IsVisible = visibilityAdapters.Any(x => x.IsColumnVisible(DriversViewModels));
            }
        }

        public TimeSpan GetDriverCurrentLapTime(IDriverInfo driverInfo)
        {
            ILapInfo lapInfo = _driverNameTimingMap.Values.FirstOrDefault(x => x.DriverId == driverInfo.DriverSessionId)?.CurrentLap;
            if (lapInfo == null)
            {
                return TimeSpan.Zero;
            }

            return lapInfo.Completed ? lapInfo.LapTime : lapInfo.CurrentlyValidProgressTime;
        }
    }
}
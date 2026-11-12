namespace SecondMonitor.Timing.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using Commands;

    using Contracts.Commands;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;

    using Foundation.Connectors;

    using LapTimings;

    using NLog;

    using Rating.Application.Rating.ViewModels;

    using ReportCreation;

    using SecondMonitor.DataModel.Extensions;

    using SessionTiming;
    using SessionTiming.Drivers;
    using SessionTiming.Drivers.ViewModel;
    using SessionTiming.ViewModel;

    using SimdataManagement.RaceSuggestion.ViewModels;

    using TimingGrid;

    using TrackRecords.Controller;

    using ViewModels;
    using ViewModels.CarStatus;
    using ViewModels.CarStatus.FuelStatus;
    using ViewModels.Factory;
    using ViewModels.Layouts;
    using ViewModels.Layouts.Factory;
    using ViewModels.PitBoard;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;
    using ViewModels.Settings.Model;
    using ViewModels.Settings.ViewModel;
    using ViewModels.Track.MapView;
    using ViewModels.Track.MapView.Controller;
    using ViewModels.TrackInfo;
    using ViewModels.TrackRecords;

    public class TimingApplicationViewModel : AbstractViewModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly DriverDetailsController _driverDetailsController;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly SessionTimingFactory _sessionTimingFactory;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ILayoutFactory _layoutFactory;
        private readonly SessionSummaryExporter _sessionSummaryExporter;
        private readonly MapViewController _mapViewController;
        private readonly CarAndFuelStatusViewModel _carAndFuelStatusViewModel;
        private readonly LapDeltaViewModel _lapDeltaViewModel;
        private readonly TrackInfoViewModel _trackInfoViewModel;
        private readonly CachedViewModelsFactory _viewModelsCache;

        private ICommand _resetCommand;

        private TimingDataViewModelResetModeEnum _shouldReset = TimingDataViewModelResetModeEnum.NoReset;

        private SessionTimingController _sessionTiming;
        private SessionType _sessionType = SessionType.Na;
        private SimulatorDataSet _lastDataSet;
        private bool _isNamesNotUnique;
        private string _notUniqueNamesMessage;
        private Stopwatch _notUniqueCheckWatch;

        private Task _refreshBasicInfoTask;
        private Task _refreshTimingCircleTask;
        private Task _refreshTimingGridTask;

        private string _connectedSource;
        private DisplaySettingsViewModel _displaySettingsViewModel;
        private IRatingApplicationViewModel _ratingApplicationViewModel;
        private CarStatusViewModel _carStatusViewModel;
        private IViewModel _mainViewModel;
        private SuggestionsViewModel _suggestionsViewModel;
        private SessionOptionsViewModel _currentSessionOptionsView;
        private int _sessionCompletedPercentage;
        private string _sessionTime;
        private string _systemTime;
        private bool _isPaused;
        private PitBoardViewModel _pitBoardViewModel;
        private bool _isSpectating;

        public TimingApplicationViewModel(ISettingsProvider settingsProvider, ITrackRecordsController trackRecordsController, DriverDetailsController driverDetailsController,
            ISessionEventProvider sessionEventProvider, SessionTimingFactory sessionTimingFactory, MapViewController mapViewController, IViewModelFactory viewModelFactory,
            ILayoutFactory layoutFactory, 
            PaceProviderFromTimingApplicationViewModel paceProviderFromTimingApplicationViewModel, SessionSummaryExporter sessionSummaryExporter, CachedViewModelsFactory cachedViewModelsFactory)
        {
            paceProviderFromTimingApplicationViewModel.SetTimingApplication(this);
            PauseCommand = new RelayCommand(SwitchPause);
            _viewModelsCache = cachedViewModelsFactory;
            SidebarViewModel = new SidebarViewModel();
            _mapViewController = mapViewController;
            _mapViewController.PropertyChanged += MapViewControllerOnPropertyChanged;
            TimingDataGridViewModel = viewModelFactory.Create<TimingDataGridViewModel>();
            SessionInfoViewModel = new SessionInfoViewModel();
            _trackInfoViewModel = viewModelFactory.Create<TrackInfoViewModel>();
            _driverDetailsController = driverDetailsController;
            _sessionEventProvider = sessionEventProvider;
            _sessionEventProvider.PlayerFinishStateChanged += SessionEventProviderOnPlayerFinishStateChanged;
            _sessionTimingFactory = sessionTimingFactory;
            _viewModelFactory = viewModelFactory;
            _layoutFactory = layoutFactory;
            _sessionSummaryExporter = sessionSummaryExporter;
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            CurrentSessionOptionsView = DisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView;
            TrackRecordsViewModel = trackRecordsController.TrackRecordsViewModel;
            _lapDeltaViewModel = _viewModelFactory.Create<LapDeltaViewModel>();

            _carAndFuelStatusViewModel = new CarAndFuelStatusViewModel()
            {
                DisplaySettingsViewModel = DisplaySettingsViewModel,
            };
            ConnectorViewModel = viewModelFactory.Create<ConnectorViewModel>();
            ClassesOverviewViewModel = _viewModelFactory.Create<ClassesOverviewViewModel>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = AssemblyName.GetAssemblyName(assembly.Location).Version.ToString();
            Title = "Second Monitor (Timing)(v" + version + " )";
        }

        public event EventHandler<SessionSummaryEventArgs> SessionCompleted;
        public event EventHandler<SessionSummaryEventArgs> PlayerFinished;

        public TimeSpan? PlayersPace => SessionTiming?.Player?.Pace;
        public TimeSpan? LeadersPace => SessionTiming?.Leader?.Pace;

        public SessionInfoViewModel SessionInfoViewModel { get; }

        public ClassesOverviewViewModel ClassesOverviewViewModel { get; }

        public PitBoardViewModel PitBoardViewModel
        {
            get => _pitBoardViewModel;
            set => SetProperty(ref _pitBoardViewModel, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                SetProperty(ref _isPaused, value);
                _lapDeltaViewModel.SetIsPaused(value);
            }
        }

        public IViewModel MainViewModel
        {
            get => _mainViewModel;
            set => SetProperty(ref _mainViewModel, value);
        }

        public SuggestionsViewModel SuggestionsViewModel
        {
            get => _suggestionsViewModel;
            set => SetProperty(ref _suggestionsViewModel, value);
        }

        public SidebarViewModel SidebarViewModel { get; }

        public DisplaySettingsViewModel DisplaySettingsViewModel
        {
            get => _displaySettingsViewModel;
            private set
            {
                _displaySettingsViewModel = value;
                NotifyPropertyChanged();
                DisplaySettingsChanged();
            }
        }

        public SessionOptionsViewModel CurrentSessionOptionsView
        {
            get => _currentSessionOptionsView;
            set
            {
                SetProperty(ref _currentSessionOptionsView, value);
                ChangeOrderingMode();
                ChangeTimeDisplayMode();
            }
        }

        public FuelOverviewViewModel FuelOverviewViewModel
        {
            get => _carAndFuelStatusViewModel.FuelOverviewViewModel;
            set => _carAndFuelStatusViewModel.FuelOverviewViewModel = value;
        }

        public bool IsNamesNotUnique
        {
            get => _isNamesNotUnique;
            private set => SetProperty(ref _isNamesNotUnique, value);
        }

        public string NotUniqueNamesMessage
        {
            get => _notUniqueNamesMessage;
            private set => SetProperty(ref _notUniqueNamesMessage, value);
        }

        public ITrackRecordsViewModel TrackRecordsViewModel { get; }

        public TimingDataGridViewModel TimingDataGridViewModel { get; }

        public int SessionCompletedPercentage
        {
            get => _sessionCompletedPercentage;
            set => SetProperty(ref _sessionCompletedPercentage, value);
        }

        public ICommand ResetCommand => _resetCommand ??= new NoArgumentCommand(ScheduleReset);

        public string SessionTime
        {
            get => _sessionTime;
            set => SetProperty(ref _sessionTime, value);
        }

        public string ConnectedSource
        {
            get => _connectedSource;
            set => SetProperty(ref _connectedSource, value);
        }

        public ConnectorViewModel ConnectorViewModel { get; }

        public string SystemTime
        {
            get => _systemTime;
            set => SetProperty(ref _systemTime, value);
        }

        public bool IsSpectating
        {
            get => _isSpectating;
            set => SetProperty(ref _isSpectating, value);
        }

        public string Title { get; }

        public SessionTimingController SessionTiming
        {
            get => _sessionTiming;
            private set => SetProperty(ref _sessionTiming, value);
        }

        public IRatingApplicationViewModel RatingApplicationViewModel
        {
            get => _ratingApplicationViewModel;
            set => SetProperty(ref _ratingApplicationViewModel, value);
        }

        private bool TerminatePeriodicTasks { get; set; }

        public ICommand PauseCommand { get; }

        public Dictionary<string, TimeSpan> GetPaceForDriversMap()
        {
            if (SessionTiming == null)
            {
                return new Dictionary<string, TimeSpan>();
            }

            return SessionTiming.Drivers.ToDictionary(x => x.Value.DriverId, x => x.Value.Pace);
        }

        public void TerminatePeriodicTask(List<Exception> exceptions)
        {
            TerminatePeriodicTasks = true;
            if (_refreshBasicInfoTask.IsFaulted && _refreshBasicInfoTask.Exception != null)
            {
                exceptions.AddRange(_refreshBasicInfoTask.Exception.InnerExceptions);
            }

            if (_refreshTimingCircleTask.IsFaulted && _refreshTimingCircleTask.Exception != null)
            {
                exceptions.AddRange(_refreshTimingCircleTask.Exception.InnerExceptions);
            }

            if (_refreshTimingGridTask.IsFaulted && _refreshTimingGridTask.Exception != null)
            {
                exceptions.AddRange(_refreshTimingGridTask.Exception.InnerExceptions);
            }
        }

        public async Task ApplyDateSet(SimulatorDataSet data)
        {
            if (data == null)
            {
                return;
            }

            IsSpectating = data.SessionInfo.SpectatingState != SpectatingState.Live;

            SidebarViewModel.IsOpenCarSettingsCommandEnable = !string.IsNullOrWhiteSpace(data.PlayerInfo?.CarName);
            ConnectedSource = data.Source;
            if (_sessionTiming == null || data.SessionInfo.SessionType == SessionType.Na || data.SessionInfo.SessionPhase == SessionPhase.Unavailable)
            {
                _carStatusViewModel?.PedalsAndGearViewModel?.ApplyDateSet(data);
                return;
            }

            _lastDataSet = data;

            if (_sessionType != data.SessionInfo.SessionType)
            {
                _shouldReset = TimingDataViewModelResetModeEnum.Automatic;
                _sessionType = _sessionTiming.SessionType;
            }

            // Reset state was detected (either reset button was pressed or timing detected a session change)
            if (_shouldReset != TimingDataViewModelResetModeEnum.NoReset)
            {
                CheckAndNotifySessionCompleted();
                await CreateTiming(data);
                _shouldReset = TimingDataViewModelResetModeEnum.NoReset;
            }

            try
            {
                CheckIdsUniques(data);
                _sessionTiming?.UpdateTiming(data);
                if (!IsPaused)
                {
                    _carStatusViewModel?.PedalsAndGearViewModel?.ApplyDateSet(data);
                    _carStatusViewModel?.HighFrequencyInformation(data);
                }
            }
            catch (DriverNotFoundException)
            {
                _shouldReset = TimingDataViewModelResetModeEnum.Automatic;
            }
        }

        public void DisplayMessage(MessageArgs e)
        {
            if (e.IsDecision)
            {
                if (MessageBox.Show(
                        e.Message,
                        "Message from connector.",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    e.Action();
                }
            }
            else
            {
                MessageBox.Show(e.Message, "Message from connector.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void Initialize()
        {
            _carStatusViewModel = _viewModelFactory.Create<CarStatusViewModel>();
            _carAndFuelStatusViewModel.CarStatusViewModel = _carStatusViewModel;
            ConnectedSource = "Not Connected";

            if (Application.Current?.Dispatcher != null && Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(ScheduleRefreshActions);
            }
            else
            {
                ScheduleRefreshActions();
            }

            OnDisplaySettingsChange(this, null);
            _shouldReset = TimingDataViewModelResetModeEnum.NoReset;

            _viewModelsCache.RegisterViewModel(CarWheelsViewModel.ViewModelLayoutName, _carStatusViewModel.PlayersWheelsViewModel);
            _viewModelsCache.RegisterViewModel(CarSystemsViewModel.ViewModelLayoutName, _carStatusViewModel.CarSystemsViewModel);
            _viewModelsCache.RegisterViewModel(DashboardViewModel.ViewModelLayoutName, _carStatusViewModel.DashboardViewModel);
            _viewModelsCache.RegisterViewModel(PedalsAndGearViewModel.ViewModelLayoutName, _carStatusViewModel.PedalsAndGearViewModel);
            _viewModelsCache.RegisterViewModel(TrackInfoViewModel.ViewModelLayoutName, _trackInfoViewModel);
            _viewModelsCache.RegisterViewModel(LapDeltaViewModel.ViewModelLayoutName, _lapDeltaViewModel);
            _viewModelsCache.RegisterViewModel(FuelOverviewViewModel.ViewModelLayoutName, FuelOverviewViewModel);
            _viewModelsCache.RegisterViewModel(ViewModels.TrackRecords.TrackRecordsViewModel.ViewModelLayoutName, TrackRecordsViewModel);
            _viewModelsCache.RegisterViewModel(SessionInfoViewModel.ViewModelLayoutName, SessionInfoViewModel);
            _viewModelsCache.RegisterViewModel(DefaultSituationOverviewViewModel.ViewModelLayoutName,
                new AutoUpdateViewModelWrapper(_mapViewController, () => _mapViewController.SituationOverviewViewModel));
            _viewModelsCache.RegisterViewModel(SidebarViewModel.ViewModelLayoutName, SidebarViewModel);
            _viewModelsCache.RegisterViewModel(Rating.Application.Rating.ViewModels.RatingApplicationViewModel.ViewModelLayoutName,
                new AutoUpdateViewModelWrapper(this, () => RatingApplicationViewModel));
            _viewModelsCache.RegisterViewModel(TimingDataGridViewModel.ViewModelLayoutName, TimingDataGridViewModel);

            CreateMainViewModel();
        }

        private static async Task SchedulePeriodicAction(Action action, Func<int> delayFunc, TimingApplicationViewModel sender, bool captureContext)
        {
            while (!sender.TerminatePeriodicTasks)
            {
                await Task.Delay(delayFunc(), CancellationToken.None).ConfigureAwait(captureContext);

                if (!sender.TerminatePeriodicTasks)
                {
                    action();
                }
            }
        }

        private void ScheduleRefreshActions()
        {
            _refreshBasicInfoTask = SchedulePeriodicAction(() => RefreshBasicInfo(_lastDataSet), () => 150, this, true);
            _refreshTimingCircleTask = SchedulePeriodicAction(() => RefreshTimingCircle(_lastDataSet), () => 100, this, false);
            _refreshTimingGridTask = SchedulePeriodicAction(() => RefreshTimingGrid(_lastDataSet), () => DisplaySettingsViewModel.RefreshRate, this, false);
        }

        private void RefreshTimingGrid(SimulatorDataSet lastDataSet)
        {
            SystemTime = DateTime.Now.ToString("HH:mm");
            if (lastDataSet == null || IsPaused)
            {
                return;
            }

            SessionTime = _sessionTiming?.SessionTime.FormatToDefault() ?? string.Empty;
            SessionCompletedPercentage = _sessionTiming?.SessionCompletedPerMiles ?? 50;
            TimingDataGridViewModel.UpdateProperties(lastDataSet);
            ClassesOverviewViewModel.FromModel(lastDataSet);
        }

        private void PaceLapsChanged()
        {
            if (_sessionTiming != null)
            {
                _sessionTiming.PaceLaps = DisplaySettingsViewModel.PaceLaps;
            }
        }

        private void CheckIdsUniques(SimulatorDataSet dataSet)
        {
            if (_notUniqueCheckWatch == null || _notUniqueCheckWatch.ElapsedMilliseconds < 10000)
            {
                return;
            }

            List<IGrouping<string, string>> namesGrouping = dataSet.DriversInfo.Select(x => x.DriverSessionId).GroupBy(x => x).ToList();
            List<string> uniqueNames = namesGrouping.Where(x => x.Count() == 1).SelectMany(x => x).ToList();
            List<string> notUniqueNames = namesGrouping.Where(x => x.Count() > 1).Select(x => x.Key).ToList();

            if (notUniqueNames.Count == 0)
            {
                IsNamesNotUnique = false;
                return;
            }

            IsNamesNotUnique = true;
            NotUniqueNamesMessage = $"Not All Driver Ids are unique: Number of unique drivers - {uniqueNames.Count}, Not unique names - {string.Join(", ", notUniqueNames)} ";
            _notUniqueCheckWatch.Restart();
        }

        private void ScheduleReset()
        {
            _shouldReset = TimingDataViewModelResetModeEnum.Manual;
        }

        private void ChangeOrderingMode()
        {
            if (_sessionTiming == null)
            {
                return;
            }

            var mode = GetOrderTypeFromSettings();
            _sessionTiming.DisplayGapToPlayerRelative = mode == DriverOrderKind.Relative;
        }

        private void ChangeTimeDisplayMode()
        {
            if (_sessionTiming == null || Application.Current?.Dispatcher == null)
            {
                return;
            }

            var mode = GetTimeDisplayTypeFromSettings();
            _sessionTiming.DisplayBindTimeRelative = mode == DisplayModeEnum.Relative;
            _sessionTiming.DisplayGapToPlayerRelative = mode == DisplayModeEnum.Relative;
        }

        private DriverOrderKind GetOrderTypeFromSettings()
        {
            return CurrentSessionOptionsView.OrderingMode;
        }

        private DisplayModeEnum GetTimeDisplayTypeFromSettings()
        {
            return CurrentSessionOptionsView.TimesDisplayMode;
        }

        private SessionOptionsViewModel GetSessionOptionOfCurrentSession(SimulatorDataSet dataSet)
        {
            if (dataSet == null)
            {
                return new SessionOptionsViewModel(_viewModelFactory);
            }

            switch (dataSet.SessionInfo.SessionType)
            {
                case SessionType.Practice:
                case SessionType.WarmUp:
                    return DisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView;
                case SessionType.Qualification:
                    return DisplaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView;
                case SessionType.Race:
                    return DisplaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView;
                default:
                    return DisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView;
            }
        }

        private void RefreshTimingCircle(SimulatorDataSet data)
        {
            if (data == null || IsPaused)
            {
                return;
            }

            _mapViewController.ApplyDateSet(data);
        }

        private void RefreshBasicInfo(SimulatorDataSet data)
        {
            if (data == null || IsPaused)
            {
                return;
            }

            _carStatusViewModel.ApplyDateSet(data);
            _trackInfoViewModel.ApplyDateSet(data);
            SessionInfoViewModel.ApplyDateSet(data);
        }

        private void SessionTimingDriverRemoved(object sender, DriverListModificationEventArgs e)
        {
            if (TerminatePeriodicTasks)
            {
                return;
            }

            TimingDataGridViewModel.RemoveDriver(e.Data);

            Application.Current.Dispatcher?.Invoke(() => { _mapViewController.RemoveDriver(e.Data.DriverInfo); });
        }

        private void SessionTimingDriverAdded(object sender, DriverListModificationEventArgs e)
        {
            if (e.Data.IsPlayer)
            {
                _lapDeltaViewModel.Driver = e.Data;
            }

            TimingDataGridViewModel.AddDriver(e.Data);
            _mapViewController.AddDriver(e.Data.DriverInfo);
            _driverDetailsController.Rebind(e.Data);
        }

        private async Task CreateTiming(SimulatorDataSet data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.Invoke(async () => await CreateTiming(data));
                return;
            }

            if (SessionTiming != null)
            {
                SessionTiming.DriverAdded -= SessionTimingDriverAdded;
                SessionTiming.DriverRemoved -= SessionTimingDriverRemoved;
                await _sessionTiming.StopControllerAsync();
            }

            bool invalidateLap = _shouldReset == TimingDataViewModelResetModeEnum.Manual ||
                                 data.SessionInfo.SessionType != SessionType.Race;
            _lastDataSet = data;
            SessionTiming = _sessionTimingFactory.Create(data, invalidateLap);
            await _sessionTiming.StartControllerAsync();
            _lapDeltaViewModel.Driver = SessionTiming.Player;
            SessionInfoViewModel.SessionTiming = _sessionTiming;
            SessionTiming.DriverAdded += SessionTimingDriverAdded;
            SessionTiming.DriverRemoved += SessionTimingDriverRemoved;
            SessionTiming.PaceLaps = DisplaySettingsViewModel.PaceLaps;

            _carStatusViewModel.Reset();
            _trackInfoViewModel.Reset();
            _mapViewController.Reset();

            InitializeGui(data);
            ChangeTimeDisplayMode();
            ChangeOrderingMode();
            ConnectedSource = data.Source;
            _notUniqueCheckWatch = Stopwatch.StartNew();
            NotifyPropertyChanged(nameof(ConnectedSource));
        }

        public async Task StartNewSession(SimulatorDataSet dataSet)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.Invoke(async () => await StartNewSession(dataSet));
                return;
            }

            ConnectedSource = dataSet.Source;
            CheckAndNotifySessionCompleted();
            if (dataSet.SessionInfo.SessionType == SessionType.Na)
            {
                return;
            }

            SessionInfoViewModel.Reset();
            UpdateCurrentSessionOption(dataSet);
            await CreateTiming(dataSet);
        }

        private void UpdateCurrentSessionOption(SimulatorDataSet data)
        {
            CurrentSessionOptionsView = GetSessionOptionOfCurrentSession(data);
        }

        private void InitializeGui(SimulatorDataSet data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => InitializeGui(data));
                return;
            }

            if (data.SessionInfo.SessionType != SessionType.Na)
            {
                TimingDataGridViewModel.MatchDriversList(_sessionTiming.Drivers.Values.ToList());
                _driverDetailsController.RebindAll(TimingDataGridViewModel.DriversViewModels.Select(x => x.DriverTiming).ToList());
            }

            _mapViewController.ApplyDateSet(data);
        }

        private void OnDisplaySettingsChange(object sender, PropertyChangedEventArgs args)
        {
            switch (args?.PropertyName)
            {
                case nameof(DisplaySettingsViewModel.PaceLaps):
                    PaceLapsChanged();
                    break;
                case nameof(SessionOptionsViewModel.OrderingMode):
                    ChangeOrderingMode();
                    break;
                case nameof(SessionOptionsViewModel.TimesDisplayMode):
                    ChangeTimeDisplayMode();
                    break;
            }
        }

        private void DisplaySettingsChanged()
        {
            DisplaySettingsViewModel newDisplaySettingsViewModel = _displaySettingsViewModel;
            newDisplaySettingsViewModel.PropertyChanged += OnDisplaySettingsChange;
            newDisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView.PropertyChanged += OnDisplaySettingsChange;
            newDisplaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView.PropertyChanged += OnDisplaySettingsChange;
            newDisplaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView.PropertyChanged += OnDisplaySettingsChange;
            newDisplaySettingsViewModel.LayoutSettingsViewModel.PropertyChanged += LayoutSettingsViewModelOnPropertyChanged;
        }

        private void LayoutSettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayoutSettingsViewModel.LayoutDescription))
            {
                CreateMainViewModel();
            }
        }

        private void SessionEventProviderOnPlayerFinishStateChanged(object sender, DataSetArgs e)
        {
            if (e.DataSet.SessionInfo.SessionType != SessionType.Race || e.DataSet.PlayerInfo.FinishStatus != DriverFinishStatus.Finished || _sessionTiming?.WasGreen != true)
            {
                return;
            }

            PlayerFinished?.Invoke(this, new SessionSummaryEventArgs(_sessionSummaryExporter.CreateSessionSummary(_sessionTiming, true)));
        }

        private void CheckAndNotifySessionCompleted()
        {
            if (_sessionTiming?.WasGreen != true || _sessionTiming.IsFinished)
            {
                return;
            }

            _sessionTiming.Finish();
            SessionCompleted?.Invoke(this, new SessionSummaryEventArgs(_sessionSummaryExporter.CreateSessionSummary(_sessionTiming, false)));
        }

        private void MapViewControllerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MapViewController.SituationOverviewViewModel))
            {
                NotifyPropertyChanged(nameof(MapViewController));
            }
        }

        private void CreateMainViewModel()
        {
            try
            {
                MainViewModel = _layoutFactory.Create(_displaySettingsViewModel.LayoutSettingsViewModel.LayoutDescription.RootElement, _viewModelsCache);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to use stored layout, using default layout.", "Error when creating layout.", MessageBoxButton.OK, MessageBoxImage.Information);
                Logger.Error(ex, "Unable to create layout, creating default");
                var defaultLayout = _displaySettingsViewModel.GetDefaultLayout();
                MainViewModel = _layoutFactory.Create(defaultLayout.RootElement, _viewModelsCache);
                _displaySettingsViewModel.LayoutSettingsViewModel.LayoutDescription = defaultLayout;
            }
        }

        private void SwitchPause()
        {
            IsPaused = !IsPaused;
        }
    }
}

namespace SecondMonitor.Timing.Application.TrackRecords.Controller
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Common.SessionTiming.Drivers.Lap;
    using Contracts.Commands;
    using Contracts.TrackRecords;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using DataModel.TrackRecords;

    using SecondMonitor.DataModel.SimulatorContent;
    using SecondMonitor.ViewModels.SimulatorContent;

    using ViewModels;
    using ViewModels.Controllers;
    using ViewModels.Factory;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;
    using ViewModels.TrackRecords;

    public class TrackRecordsController : AbstractController, ITrackRecordsController, ITrackRecordsProvider
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IWindowService _windowService;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IDialogService _dialogService;
        private readonly ISimulatorContentProvider _simulatorContentProvider;
        private readonly TrackRecordsRepository _trackRecordsRepository;
        private SimulatorsRecords _simulatorsRecords;
        private SimulatorRecords _currentSimulatorRecords;
        private TrackRecord _currentTrackRecords;
        private NamedRecordSet _currentVehicleRecordSet;
        private NamedRecordSet _currentClassRecordSet;
        private bool _isEnabled;
        private bool _showRecordsForSession;
        private SessionType _currentSessionType;
        private RecordsOverviewWindowViewModel _recordsOverviewWindowViewModel;
        private Window _recordsOverviewWindow;
        private Task _saveTask;

        public TrackRecordsController(ISettingsProvider settingsProvider,
            ITrackRecordsRepositoryFactory trackRecordsRepositoryFactory, IViewModelFactory viewModelFactory,
            IWindowService windowService, ISessionEventProvider sessionEventProvider, IDialogService dialogService, ISimulatorContentProvider simulatorContentProvider)
            : base(viewModelFactory)
        {
            TrackRecordsViewModel = viewModelFactory.Create<ITrackRecordsViewModel>();
            _settingsProvider = settingsProvider;

            _windowService = windowService;
            _sessionEventProvider = sessionEventProvider;
            _dialogService = dialogService;
            _simulatorContentProvider = simulatorContentProvider;
            _trackRecordsRepository = trackRecordsRepositoryFactory.Create();
            BindCommands();
        }

        public ITrackRecordsViewModel TrackRecordsViewModel { get; }

        public override Task StartControllerAsync()
        {
            _simulatorsRecords = _trackRecordsRepository.LoadOrCreateNew();
            ClearUpEntriesWithoutNames();
            _settingsProvider.DisplaySettingsViewModel.TrackRecordsSettingsViewModel.PropertyChanged +=
                TrackRecordsSettingsViewModelOnPropertyChanged;
            InitializeFromSettings();
            _sessionEventProvider.TrackChanged += SessionEventProviderOnPlayerPropertiesChanged;
            _sessionEventProvider.PlayerPropertiesChanged += SessionEventProviderOnPlayerPropertiesChanged;
            return Task.CompletedTask;
        }

        public override async Task StopControllerAsync()
        {
            if (_saveTask != null)
            {
                await _saveTask;
            }

            _sessionEventProvider.PlayerPropertiesChanged -= SessionEventProviderOnPlayerPropertiesChanged;
            _settingsProvider.DisplaySettingsViewModel.TrackRecordsSettingsViewModel.PropertyChanged -=
                TrackRecordsSettingsViewModelOnPropertyChanged;
            _trackRecordsRepository.Save(_simulatorsRecords);
        }

        private void ClearUpEntriesWithoutNames()
        {
            foreach (SimulatorRecords simulatorsRecordsSimulatorRecord in _simulatorsRecords.SimulatorRecords)
            {
                foreach (TrackRecord trackRecord in simulatorsRecordsSimulatorRecord.TrackRecords)
                {
                    trackRecord.ClassRecords.RemoveAll(x => string.IsNullOrEmpty(x.Name));
                    trackRecord.VehicleRecords.RemoveAll(x => string.IsNullOrEmpty(x.Name));
                }
            }
        }

        private void InitializeFromSettings()
        {
            _showRecordsForSession = _settingsProvider.DisplaySettingsViewModel.TrackRecordsSettingsViewModel
                .DisplayRecordForCurrentSessionType;
            _isEnabled = _settingsProvider.DisplaySettingsViewModel.TrackRecordsSettingsViewModel.IsEnabled;
            TrackRecordsViewModel.IsVisible = _isEnabled;
            RefreshViewModel();
        }

        private void TrackRecordsSettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InitializeFromSettings();
        }

        public void OnSessionStarted(SimulatorDataSet dataSet)
        {
            InitializeCurrentSets(dataSet);
        }

        public bool EvaluateFastestLapCandidate(ILapInfo lapInfo)
        {
            if (!lapInfo.Valid || !lapInfo.Driver.IsPlayer || !_isEnabled || _sessionEventProvider.LastDataSet.SessionInfo.SpectatingState != SpectatingState.Live)
            {
                return false;
            }

            if (_currentTrackRecords.TrackName != lapInfo.Driver.Session.LastSet.SessionInfo.TrackInfo.TrackFullName ||
                _currentClassRecordSet.Name != lapInfo.Driver.CarClassName ||
                _currentVehicleRecordSet.Name != lapInfo.Driver.CarName)
            {
                InitializeCurrentSets(lapInfo.Driver.Session.LastSet);
            }

            bool isTrackRecord = EvaluateAsTrackRecord(lapInfo);
            bool isClassRecord = EvaluateAsClassRecord(lapInfo);
            bool isVehicleRecord = EvaluateAsVehicleRecord(lapInfo);

            TrackRecordsViewModel.TrackRecord.IsHighlighted = isTrackRecord;
            TrackRecordsViewModel.ClassRecord.IsHighlighted = isClassRecord;
            TrackRecordsViewModel.VehicleRecord.IsHighlighted = isVehicleRecord;

            if (isVehicleRecord || isClassRecord || isTrackRecord)
            {
                RefreshViewModel();
                _saveTask = Task.Run(() => _trackRecordsRepository.Save(_simulatorsRecords));
            }

            return isVehicleRecord || isClassRecord || isTrackRecord;
        }

        public bool TryGetOverallBestRecord(string simulatorName, string trackFullName, SessionType sessionType,
            out RecordEntryDto recordEntry)
        {
            var simulatorSet = _simulatorsRecords.GetOrCreateSimulatorRecords(simulatorName);
            var trackRecordSet = simulatorSet.TrackRecords.FirstOrDefault(x => x.TrackName == trackFullName);
            if (trackRecordSet == null)
            {
                recordEntry = null;
                return false;
            }

            recordEntry = trackRecordSet.OverallRecord.GetProperEntry(sessionType);
            return recordEntry != null;
        }

        public bool TryGetCarBestRecord(string simulatorName, string trackFullName, string carName,
            SessionType sessionType, out RecordEntryDto recordEntry)
        {
            var simulatorSet = _simulatorsRecords.GetOrCreateSimulatorRecords(simulatorName);
            var trackRecordSet = simulatorSet.TrackRecords.FirstOrDefault(x => x.TrackName == trackFullName);
            if (trackRecordSet == null)
            {
                recordEntry = null;
                return false;
            }

            var carRecord = trackRecordSet.GetOrCreateVehicleRecord(carName);
            recordEntry = carRecord.GetProperEntry(sessionType);
            return recordEntry != null;
        }

        public bool TryGetClassBestRecord(string simulatorName, string trackFullName, string className,
            SessionType sessionType, out RecordEntryDto recordEntry)
        {
            var simulatorSet = _simulatorsRecords.GetOrCreateSimulatorRecords(simulatorName);
            var trackRecordSet = simulatorSet.TrackRecords.FirstOrDefault(x => x.TrackName == trackFullName);
            if (trackRecordSet == null)
            {
                recordEntry = null;
                return false;
            }

            var carRecord = trackRecordSet.GetOrCreateClassRecord(className);
            recordEntry = carRecord.GetProperEntry(sessionType);
            return recordEntry != null;
        }

        private void SessionEventProviderOnPlayerPropertiesChanged(object sender, DataSetArgs e)
        {
            InitializeCurrentSets(e.DataSet);
        }

        private bool EvaluateAsTrackRecord(ILapInfo lapInfo)
        {
            var sessionType = lapInfo.Driver.Session.SessionType;
            if (Evaluate(lapInfo, _currentTrackRecords.OverallRecord.GetProperEntry(sessionType)))
            {
                var newRecordEntry = FromLap(lapInfo);
                _currentTrackRecords.OverallRecord.SetProperEntry(sessionType, newRecordEntry);
                return GetCurrentTrackRecord() == newRecordEntry;
            }

            return false;
        }

        private bool EvaluateAsVehicleRecord(ILapInfo lapInfo)
        {
            var sessionType = lapInfo.Driver.Session.SessionType;
            if (Evaluate(lapInfo, _currentVehicleRecordSet.GetProperEntry(sessionType)))
            {
                RecordEntryDto newRecordEntry = FromLap(lapInfo);
                _currentVehicleRecordSet.SetProperEntry(sessionType, newRecordEntry);
                return GetCurrentVehicleRecord() == newRecordEntry;
            }

            return false;
        }

        private bool EvaluateAsClassRecord(ILapInfo lapInfo)
        {
            var sessionType = lapInfo.Driver.Session.SessionType;
            if (Evaluate(lapInfo, _currentClassRecordSet.GetProperEntry(sessionType)))
            {
                var newRecordEntry = FromLap(lapInfo);
                _currentClassRecordSet.SetProperEntry(sessionType, newRecordEntry);
                return GetCurrentClassRecord() == newRecordEntry;
            }

            return false;
        }

        private bool Evaluate(ILapInfo lapInfo, RecordEntryDto recordEntryDto)
        {
            if (recordEntryDto == null || recordEntryDto.LapTime > lapInfo.LapTime)
            {
                return true;
            }

            return false;
        }

        private void InitializeCurrentSets(SimulatorDataSet dataSet)
        {
            if (dataSet == null || dataSet.SessionInfo.SessionType == SessionType.Na || string.IsNullOrEmpty(dataSet.Source) ||
                dataSet.PlayerInfo == null)
            {
                return;
            }

            _currentSessionType = dataSet.SessionInfo.SessionType;
            _currentSimulatorRecords = _simulatorsRecords.GetOrCreateSimulatorRecords(dataSet.Source);
            _currentTrackRecords =
                _currentSimulatorRecords.GetOrCreateTrackRecord(dataSet.SessionInfo.TrackInfo.TrackFullName);
            _currentVehicleRecordSet = _currentTrackRecords.GetOrCreateVehicleRecord(dataSet.PlayerInfo.CarName);
            _currentClassRecordSet = _currentTrackRecords.GetOrCreateClassRecord(dataSet.PlayerInfo.CarClassName);

            RefreshViewModel();
            TrackRecordsViewModel.TrackRecord.IsHighlighted = false;
            TrackRecordsViewModel.ClassRecord.IsHighlighted = false;
            TrackRecordsViewModel.VehicleRecord.IsHighlighted = false;
        }

        private void RefreshViewModel()
        {
            if (_currentTrackRecords == null || _currentClassRecordSet == null || _currentVehicleRecordSet == null)
            {
                return;
            }

            TrackRecordsViewModel.TrackRecord.FromModel(GetCurrentTrackRecord());
            TrackRecordsViewModel.ClassRecord.FromModel(GetCurrentClassRecord());
            TrackRecordsViewModel.VehicleRecord.FromModel(GetCurrentVehicleRecord());
        }

        private RecordEntryDto GetCurrentVehicleRecord()
        {
            return GetCorrectRecordEntry(_currentVehicleRecordSet);
        }

        private RecordEntryDto GetCurrentClassRecord()
        {
            return GetCorrectRecordEntry(_currentClassRecordSet);
        }

        private RecordEntryDto GetCurrentTrackRecord()
        {
            return GetCorrectRecordEntry(_currentTrackRecords.OverallRecord);
        }

        private RecordEntryDto GetCorrectRecordEntry(RecordSet recordSet)
        {
            return _showRecordsForSession ? recordSet.GetProperEntry(_currentSessionType) : recordSet.GetOverAllBest();
        }

        private RecordEntryDto FromLap(ILapInfo lapInfo)
        {
            return _simulatorContentProvider.TryGetTrack(_currentSimulatorRecords.SimulatorName, _currentTrackRecords.TrackName, out Track track) ?
                new RecordEntryDto()
                {
                    CarClass = lapInfo.Driver.CarClassName,
                    CarName = lapInfo.Driver.CarName,
                    CarDisplayName = lapInfo.Driver.CarDisplayName,
                    IsPlayer = true,
                    LapTime = lapInfo.LapTime,
                    PlayerName = lapInfo.Driver.DriverLongName,
                    RecordDate = DateTime.Now,
                    SessionType = lapInfo.Driver.Session.SessionType,
                    AverageSpeedInKph = (track.LapDistance / 1000.0) / lapInfo.LapTime.TotalHours
                }
                :
                new RecordEntryDto()
                {
                    CarClass = lapInfo.Driver.CarClassName,
                    CarName = lapInfo.Driver.CarName,
                    CarDisplayName = lapInfo.Driver.CarDisplayName,
                    IsPlayer = true,
                    LapTime = lapInfo.LapTime,
                    PlayerName = lapInfo.Driver.DriverLongName,
                    RecordDate = DateTime.Now,
                    SessionType = lapInfo.Driver.Session.SessionType,
                    AverageSpeedInKph = 0
                };
        }

        private void BindCommands()
        {
            TrackRecordsViewModel.OpenTracksRecordsCommand = new RelayCommand(OpenTracksRecords);
            TrackRecordsViewModel.OpenClassRecordsCommand = new RelayCommand(OpenClassRecords);
            TrackRecordsViewModel.OpenVehiclesRecordsCommands = new RelayCommand(OpenVehicleRecords);
            Bind<RecordsOverviewWindowViewModel>().Command(x => x.DeleteAllRecordsForSimulator).To(DeleteAllRecordsForSelectedSimulator);
            Bind<ClassRecordsViewModel>().Command(x => x.RemoveClassCommand).To<ClassRecordsViewModel>(RemoveRecordsForClass);
            Bind<TracksAllRecordsViewModel>().Command(x => x.DeleteAllRecordsForTrack).To<TracksAllRecordsViewModel>(RemoveRecordsForTrack);
        }

        private void OpenTracksRecords()
        {
            OpenRecordsOverview();
        }

        private void OpenVehicleRecords()
        {
            OpenRecordsOverview();
        }

        private void OpenClassRecords()
        {
            OpenRecordsOverview();
        }

        private void OpenRecordsOverview()
        {
            if (_recordsOverviewWindow != null)
            {
                _recordsOverviewWindow.Focus();
                _recordsOverviewWindowViewModel.DeleteAllRecordsForSimulator = null;
                return;
            }

            _recordsOverviewWindowViewModel = ViewModelFactory.Create<RecordsOverviewWindowViewModel>();
            _recordsOverviewWindowViewModel.FromModel(_simulatorsRecords);

            if (_currentTrackRecords != null)
            {
                _recordsOverviewWindowViewModel.SelectedSimulator = _currentSimulatorRecords.SimulatorName;
                _recordsOverviewWindowViewModel.SelectedTrack =
                    _recordsOverviewWindowViewModel.AvailableTracks.FirstOrDefault(x =>
                        x.TrackName == _currentTrackRecords.TrackName);
            }

            _recordsOverviewWindow = _windowService.OpenWindow(_recordsOverviewWindowViewModel, "Records Overview",
                WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterOwner,
                _ => OnRecordsWindowClosed());
        }

        private void OnRecordsWindowClosed()
        {
            _recordsOverviewWindow = null;
            _recordsOverviewWindowViewModel = null;
        }

        private void DeleteAllRecordsForSelectedSimulator()
        {
            if (_recordsOverviewWindowViewModel == null)
            {
                return;
            }

            string simulator = _recordsOverviewWindowViewModel.SelectedSimulator;
            if (!_dialogService.ShowYesNoDialog("Delete All Records",
                    $"This will delete all track records for simulator {simulator}. Do you wish to continue?"))
            {
                return;
            }

            _simulatorsRecords.RemoveRecordsForSimulator(simulator);
            _recordsOverviewWindowViewModel.FromModel(_simulatorsRecords);
            _recordsOverviewWindowViewModel.SelectedSimulator = simulator;
            ClearCurrentRecords();
            InitializeCurrentSets(_sessionEventProvider.LastDataSet);
            RefreshViewModel();
        }

        private void RemoveRecordsForClass(ClassRecordsViewModel carClassRecordsViewModel)
        {
            if (_recordsOverviewWindowViewModel == null)
            {
                return;
            }

            string simulator = _recordsOverviewWindowViewModel.SelectedSimulator;
            string track = carClassRecordsViewModel.TrackName;
            string carClass = carClassRecordsViewModel.ClassName;

            if (!_dialogService.ShowYesNoDialog("Delete All Records",
                    $"This will delete track records for simulator: {simulator}\nTrack: {track}\nClass: {carClass}\nDo you wish to continue?"))
            {
                return;
            }

            _simulatorsRecords.RemoveRecordsForSimulator(simulator, track, carClass);

            if (_dialogService.ShowYesNoDialog("Delete All Records",
                    $"Would you also like to delete records for Class {carClass} on All Other Tracks?"))
            {
                _simulatorsRecords.RemoveClassRecordsForSimulator(simulator, carClass);
            }

            _recordsOverviewWindowViewModel.FromModel(_simulatorsRecords);
            _recordsOverviewWindowViewModel.SelectedSimulator = simulator;
            _recordsOverviewWindowViewModel.SelectedTrack = _recordsOverviewWindowViewModel.AvailableTracks.FirstOrDefault(x =>
                x.TrackName == track);
            ClearCurrentRecords();
            InitializeCurrentSets(_sessionEventProvider.LastDataSet);
            RefreshViewModel();
        }

        private void RemoveRecordsForTrack(TracksAllRecordsViewModel obj)
        {
            if (_recordsOverviewWindowViewModel?.TracksAllRecordsViewModel == null || _recordsOverviewWindowViewModel.SelectedTrack == null)
            {
                return;
            }

            string simulator = _recordsOverviewWindowViewModel.SelectedSimulator;
            string selectedTrack = _recordsOverviewWindowViewModel.TracksAllRecordsViewModel.TrackName;
            if (!_dialogService.ShowYesNoDialog("Delete All Records",
                    $"This will delete track records for simulator: {simulator}\nTrack: {selectedTrack}\nDo you wish to continue?"))
            {
                return;
            }

            _simulatorsRecords.RemoveClasTrackForSimulator(simulator, selectedTrack);

            _recordsOverviewWindowViewModel.FromModel(_simulatorsRecords);
            _recordsOverviewWindowViewModel.SelectedSimulator = simulator;
            _recordsOverviewWindowViewModel.SelectedTrack = _recordsOverviewWindowViewModel.AvailableTracks.FirstOrDefault();
            ClearCurrentRecords();
            InitializeCurrentSets(_sessionEventProvider.LastDataSet);
            RefreshViewModel();
        }

        private void ClearCurrentRecords()
        {
            _currentTrackRecords = null;
            _currentVehicleRecordSet = null;
            _currentClassRecordSet = null;
        }
    }
}
namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver
{
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.DataModel;
    using Common.DataModel.Player;
    using Contracts.Commands;
    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary;
    using NLog;
    using RatingProvider;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SimulatorRating;
    using States.Context;
    using ViewModels;

    public class RaceObserverController : IRaceObserverController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static readonly string[] SupportedSimulators =
        {
            SimulatorsNameMap.R3ESimName, SimulatorsNameMap.AcSimName, SimulatorsNameMap.AmsSimName, SimulatorsNameMap.Rf2SimName, SimulatorsNameMap.PCars2SimName,
            SimulatorsNameMap.Ams2SimName, SimulatorsNameMap.AccSimName, SimulatorsNameMap.PCarsSimName, SimulatorsNameMap.Gtr2SimName, SimulatorsNameMap.LmUSimName, SimulatorsNameMap.F12024
        };

        private readonly ISimulatorRatingController _simulatorRatingController;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private readonly SharedContext _sharedContext;
        private string _currentClass;

        private IRatingApplicationViewModel _ratingApplicationViewModel;
        private string _playersName;

        public RaceObserverController(ISimulatorRatingController simulatorRatingController, ISettingsProvider settingsProvider, SharedContext sharedContext)
        {
            _simulatorRatingController = simulatorRatingController;
            _sharedContext = sharedContext;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            CurrentSimulator = string.Empty;
            _currentClass = string.Empty;
            _playersName = string.Empty;
        }

        public string CurrentSimulator { get; private set; }

        public IRatingApplicationViewModel RatingApplicationViewModel
        {
            get => _ratingApplicationViewModel;
            set
            {
                UnsubscribeViewModel();
                _ratingApplicationViewModel = value;
                SubscribeViewModel();
            }
        }

        public static bool IsSimulatorSupported(string simulatorName)
        {
            return SupportedSimulators.Contains(simulatorName);
        }

        public async Task StartControllerAsync()
        {
            await _simulatorRatingController.StartControllerAsync();
            SubscribeSimulatorRatingController();
        }

        public async Task StopControllerAsync()
        {
            if (_simulatorRatingController != null)
            {
                await _simulatorRatingController.StopControllerAsync();
            }

            if (RatingApplicationViewModel != null)
            {
                RatingApplicationViewModel.PropertyChanged -= RatingApplicationViewModelOnPropertyChanged;
            }

            if (_simulatorRatingController != null)
            {
                UnSubscribeSimulatorRatingController();
            }
        }

        private void SubscribeViewModel()
        {
            if (RatingApplicationViewModel == null)
            {
                return;
            }

            RatingApplicationViewModel.AssignSelectedDifficultyCommand = new RelayCommand(AssignSelectedDifficulty);
            RatingApplicationViewModel.PropertyChanged += RatingApplicationViewModelOnPropertyChanged;
        }

        private async void AssignSelectedDifficulty()
        {
            if (_simulatorRatingController == null || string.IsNullOrEmpty(_currentClass))
            {
                return;
            }

            int selectedDifficulty = RatingApplicationViewModel.Difficulty;
            await _simulatorRatingController.SetClassRatingByDifficulty(_currentClass, selectedDifficulty);
        }

        private void UnsubscribeViewModel()
        {
            if (RatingApplicationViewModel == null)
            {
                return;
            }

            RatingApplicationViewModel.AssignSelectedDifficultyCommand = null;
            RatingApplicationViewModel.PropertyChanged -= RatingApplicationViewModelOnPropertyChanged;
        }

        private void RatingApplicationViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_simulatorRatingController == null)
            {
                return;
            }

            if (e.PropertyName == nameof(RatingApplicationViewModel.SelectedClass) && _sharedContext.CurrentState.CanUserSelectClass)
            {
                _currentClass = RatingApplicationViewModel.SelectedClass;
                OnClassChanged();
            }

            if (e.PropertyName == nameof(RatingApplicationViewModel.UseSuggestedDifficulty) && RatingApplicationViewModel.UseSuggestedDifficulty)
            {
                RatingApplicationViewModel.Difficulty = RatingApplicationViewModel.DifficultyRating.OriginalModel.Difficulty;
            }

            if (e.PropertyName == nameof(RatingApplicationViewModel.UseSuggestedDifficulty))
            {
                _simulatorRatingController.SetSelectedDifficulty(RatingApplicationViewModel.Difficulty, !RatingApplicationViewModel.UseSuggestedDifficulty, _currentClass);
            }

            if (e.PropertyName == nameof(RatingApplicationViewModel.Difficulty))
            {
                _sharedContext.UserSelectedDifficulty = RatingApplicationViewModel.Difficulty;
                _simulatorRatingController.SetSelectedDifficulty(RatingApplicationViewModel.Difficulty, !RatingApplicationViewModel.UseSuggestedDifficulty, _currentClass);
            }
        }

        public void Reset()
        {
            ResetToInitialState();
            RefreshViewModelByState();
        }

        public async Task NotifySessionCompletion(SessionSummary sessionSummary)
        {
            if (_simulatorRatingController.CurrentSimRatingConfiguration == null)
            {
                return;
            }

            Logger.Info($"Notified Session Completed, current state - {_sharedContext.CurrentState.GetType()}");

            if (sessionSummary.SessionType == SessionType.Race && _sharedContext.DifficultyRating == null)
            {
                Logger.Warn("Race completed but difficulty rating is null");
                (DriversRating ClassRating, DriversRating DifficultyRating) rating = _simulatorRatingController.GetPlayerRating(_currentClass);
                _sharedContext.DifficultyRating = rating.DifficultyRating;
                _sharedContext.ClassRating = rating.ClassRating;
            }

            await _sharedContext.CurrentState.DoSessionCompletion(sessionSummary);
            RefreshViewModelByState();
        }

        public async Task NotifyDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            CheckSimulatorClassChange(simulatorDataSet);
            await _sharedContext.CurrentState.DoDataLoaded(simulatorDataSet);

            _playersName = simulatorDataSet.PlayerInfo.DriverLongName;
            RefreshViewModelByState();
        }

        private void CheckSimulatorClassChange(SimulatorDataSet simulatorDataSet)
        {
            if (simulatorDataSet.Source != CurrentSimulator && !string.IsNullOrWhiteSpace(simulatorDataSet.Source) && !SimulatorsNameMap.IsNotConnected(simulatorDataSet.Source))
            {
                CurrentSimulator = simulatorDataSet.Source;
                _currentClass = string.Empty;
                OnSimulatorChanged();
                OnClassChanged();
                return;
            }

            if (simulatorDataSet.PlayerInfo == null)
            {
                return;
            }

            if (simulatorDataSet.PlayerInfo.CarClassName == _currentClass || string.IsNullOrWhiteSpace(simulatorDataSet.PlayerInfo.CarClassName) || _sharedContext.CurrentState.CanUserSelectClass)
            {
                return;
            }

            _currentClass = simulatorDataSet.PlayerInfo.CarClassName;
            RatingApplicationViewModel.AddSelectableClass(_currentClass);
            RatingApplicationViewModel.SelectedClass = _currentClass;
            OnClassChanged();
        }

        private void OnClassChanged()
        {
            RefreshClassRatingOnVm();
        }

        private void OnSimulatorChanged()
        {
            if (!IsSimulatorSupported(CurrentSimulator))
            {
                RatingApplicationViewModel.IsEnabled = false;
                RatingApplicationViewModel.CollapsedMessage = $"{CurrentSimulator}, is not supported";
                return;
            }

            _simulatorRatingController.ChangeSimulator(CurrentSimulator);

            RatingApplicationViewModel.IsEnabled = true;
            RatingApplicationViewModel.InitializeAiDifficultySelection(_simulatorRatingController.MinimumAiDifficulty, _simulatorRatingController.MaximumAiDifficulty);
            ResetToInitialState();
            RefreshClassesOnVm();
            RefreshSimulatorRatingOnVm();
            if (!string.IsNullOrEmpty(_simulatorRatingController.LastPlayedClass))
            {
                RatingApplicationViewModel.SelectedClass = _simulatorRatingController.LastPlayedClass;
            }
        }

        private void ResetToInitialState()
        {
            if (!IsSimulatorSupported(CurrentSimulator))
            {
                return;
            }

            _sharedContext.SwitchToInitialState();
            _sharedContext.UserSelectedDifficulty = RatingApplicationViewModel.Difficulty;
            _sharedContext.SimulatorRatingController = _simulatorRatingController;
            _sharedContext.SimulatorRating = _simulatorRatingController.GetPlayerOverallRating();
        }

        private void RefreshClassesOnVm()
        {
            RatingApplicationViewModel.ClearSelectableClasses();
            _simulatorRatingController.GetAllKnowClassNames().OrderBy(x => x).ForEach(RatingApplicationViewModel.AddSelectableClass);
            RatingApplicationViewModel.SelectedClass = RatingApplicationViewModel.SelectableClasses.FirstOrDefault();
        }

        private void RefreshSimulatorRatingOnVm()
        {
            if (_simulatorRatingController == null)
            {
                return;
            }

            RatingApplicationViewModel.SimulatorRating.FromModel(_simulatorRatingController.GetPlayerOverallRating());
        }

        private void RefreshClassRatingOnVm()
        {
            if (string.IsNullOrWhiteSpace(_currentClass))
            {
                return;
            }

            var classRating = _simulatorRatingController.GetPlayerRating(_currentClass);
            _sharedContext.DifficultyRating = classRating.DifficultyRating;
            _sharedContext.ClassRating = classRating.ClassRating;
            RatingApplicationViewModel.ClassRating.FromModel(classRating.ClassRating);
            RatingApplicationViewModel.DifficultyRating.FromModel(classRating.DifficultyRating);
            DifficultySettings difficultySettings = _simulatorRatingController.GetDifficultySettings(_currentClass);

            RatingApplicationViewModel.UseSuggestedDifficulty = !difficultySettings.WasUserSelected;
            RatingApplicationViewModel.Difficulty = difficultySettings.SelectedDifficulty;
            if (_sharedContext.RaceContext?.FieldRating != null && !string.IsNullOrEmpty(_playersName))
            {
                _sharedContext.RaceContext.FieldRating[_playersName] = classRating.ClassRating;
            }
        }

        private void RefreshViewModelByState()
        {
            RatingApplicationViewModel.IsClassSelectionEnable = _sharedContext.CurrentState.CanUserSelectClass;
            RatingApplicationViewModel.SessionPhaseKind = _sharedContext.CurrentState.SessionPhaseKind;
            RatingApplicationViewModel.SessionKind = _sharedContext.CurrentState.SessionKind;
            RatingApplicationViewModel.SessionTextInformation = _sharedContext.CurrentState.SessionDescription;
            RatingApplicationViewModel.SimulatorRating.RatingChangeVisible = _sharedContext.CurrentState.ShowRatingChange;
            RatingApplicationViewModel.ClassRating.RatingChangeVisible = _sharedContext.CurrentState.ShowRatingChange;
            RatingApplicationViewModel.DifficultyRating.RatingChangeVisible = _sharedContext.CurrentState.ShowRatingChange;
        }

        private void SubscribeSimulatorRatingController()
        {
            if (_simulatorRatingController == null)
            {
                return;
            }

            _simulatorRatingController.ClassRatingChanged += SimulatorRatingControllerOnClassRatingChanged;
            _simulatorRatingController.SimulatorRatingChanged += SimulatorRatingControllerOnSimulatorRatingChanged;
            _simulatorRatingController.ClassDifficultyRatingChanged += SimulatorRatingControllerOnClassDifficultyRatingChanged;
        }

        private void UnSubscribeSimulatorRatingController()
        {
            if (_simulatorRatingController == null)
            {
                return;
            }

            _simulatorRatingController.ClassRatingChanged -= SimulatorRatingControllerOnClassRatingChanged;
            _simulatorRatingController.SimulatorRatingChanged -= SimulatorRatingControllerOnSimulatorRatingChanged;
        }

        private void SimulatorRatingControllerOnSimulatorRatingChanged(object sender, RatingChangeArgs e)
        {
            RefreshSimulatorRatingOnVm();
            _sharedContext.SimulatorRating = e.NewRating;
            _ratingApplicationViewModel.SimulatorRating.RatingChange = e.RatingChange;
        }

        private void SimulatorRatingControllerOnClassRatingChanged(object sender, RatingChangeArgs e)
        {
            RefreshClassRatingOnVm();
            _ratingApplicationViewModel.ClassRating.RatingChange = e.RatingChange;
        }

        private void SimulatorRatingControllerOnClassDifficultyRatingChanged(object sender, RatingChangeArgs e)
        {
            RefreshClassRatingOnVm();
            _ratingApplicationViewModel.DifficultyRating.RatingChange = e.RatingChange;
        }

        public bool TryGetRatingForDriverCurrentSession(string driverId, out DriversRating driversRating)
        {
            if (_displaySettingsViewModel.RatingSettingsViewModel.IsEnabled == false)
            {
                driversRating = new DriversRating();
                return false;
            }

            return _sharedContext.CurrentState.TryGetDriverRating(driverId, out driversRating);
        }
    }
}
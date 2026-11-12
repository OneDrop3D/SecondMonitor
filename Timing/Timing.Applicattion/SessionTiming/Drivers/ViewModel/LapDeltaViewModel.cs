namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.ViewModel
{
    using System;
    using System.ComponentModel;

    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Timing.Common.SessionTiming;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.Model;

    using ViewModels;
    using ViewModels.Settings.ViewModel;

    public class LapDeltaViewModel : AbstractViewModel, IDisposable
    {
        private readonly ISessionInfoProvider _sessionInfoProvider;
        public const string ViewModelLayoutName = "Lap Delta";
        private DriverTiming _driver;
        private DisplaySettingsViewModel _displaySettingsViewModel;

        public LapDeltaViewModel(ISettingsProvider settingsProvider, ILapEventProvider lapEventProvider, ISessionInfoProvider sessionInfoProvider)
        {
            _sessionInfoProvider = sessionInfoProvider;
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            TopLapComparatorViewModel = new LapPortionTimesComparatorViewModel();
            BottomLapComparatorViewModel = new LapPortionTimesComparatorViewModel();
            RefreshReferenceLaps();
            DisplaySettingsViewModel.DeltaLapSettingViewModel.PropertyChanged += DeltaLapSettingViewModelOnPropertyChanged;
            DisplaySettingsViewModel.PropertyChanged += DisplaySettingsViewModelOnPropertyChanged;
            lapEventProvider.BestClassLapChanged += LapEventProviderOnBestClassLapChanged;

            TopLapComparatorViewModel.AnimateDeltaTimes = _displaySettingsViewModel.AnimateDeltaTimes;
            BottomLapComparatorViewModel.AnimateDeltaTimes = _displaySettingsViewModel.AnimateDeltaTimes;
        }

        public LapPortionTimesComparatorViewModel BottomLapComparatorViewModel { get; }

        public LapPortionTimesComparatorViewModel TopLapComparatorViewModel { get; }

        public DisplaySettingsViewModel DisplaySettingsViewModel
        {
            get => _displaySettingsViewModel;
            set => SetProperty(ref _displaySettingsViewModel, value);
        }

        public DriverTiming Driver
        {
            get => _driver;
            set
            {
                UnSubscribeToDriverEvents();
                _driver = value;
                RefreshReferenceLaps();
                SubscribeToDriverEvents();
            }
        }

        private void SubscribeToDriverEvents()
        {
            if (_driver == null)
            {
                return;
            }

            _driver.NewLapStarted += DriverOnLapCompletedOrInvalidated;
            _driver.LapCompleted += DriverOnLapCompletedOrInvalidated;
            _driver.LapInvalidated += DriverOnLapCompletedOrInvalidated;
        }

        private void UnSubscribeToDriverEvents()
        {
            if (_driver == null)
            {
                return;
            }

            _driver.NewLapStarted -= DriverOnLapCompletedOrInvalidated;
            _driver.LapCompleted -= DriverOnLapCompletedOrInvalidated;
            _driver.LapInvalidated -= DriverOnLapCompletedOrInvalidated;
        }

        private void DriverOnLapCompletedOrInvalidated(object sender, LapEventArgs e)
        {
            RefreshReferenceLaps();
        }

        private void RefreshReferenceLaps()
        {
            SimulatorDataSet dataSet = _sessionInfoProvider.SessionInfo?.LastSet;

            if (dataSet == null || _driver == null)
            {
                TopLapComparatorViewModel.ChangeReferencedLaps(null, null);
                BottomLapComparatorViewModel.ChangeReferencedLaps(null, null);
                return;
            }

            (LapReferenceKind TopBarReference, LapReferenceKind BottomBarReference) barReferences = GetBarsReference(dataSet.SessionInfo.SessionType);

            RefreshReferenceLaps(TopLapComparatorViewModel, barReferences.TopBarReference);
            RefreshReferenceLaps(BottomLapComparatorViewModel, barReferences.BottomBarReference);
        }

        private (LapReferenceKind TopBarReference, LapReferenceKind BottomBarReference) GetBarsReference(SessionType sessionType)
        {
            LapReferenceKind topDeltaBarKind;
            LapReferenceKind bottomDeltaBarKind;

            switch (sessionType)
            {
                case SessionType.Qualification:
                    topDeltaBarKind = _displaySettingsViewModel.DeltaLapSettingViewModel.QualificationTopLapReferenceKind.Value;
                    bottomDeltaBarKind = _displaySettingsViewModel.DeltaLapSettingViewModel.QualificationBottomLapReferenceKind.Value;
                    break;
                case SessionType.Race:
                    topDeltaBarKind = _displaySettingsViewModel.DeltaLapSettingViewModel.RaceTopLapReferenceKind.Value;
                    bottomDeltaBarKind = _displaySettingsViewModel.DeltaLapSettingViewModel.RaceBottomLapReferenceKind.Value;
                    break;
                case SessionType.WarmUp:
                case SessionType.Na:
                case SessionType.Practice:
                default:
                    topDeltaBarKind = _displaySettingsViewModel.DeltaLapSettingViewModel.PracticeTopLapReferenceKind.Value;
                    bottomDeltaBarKind = _displaySettingsViewModel.DeltaLapSettingViewModel.PracticeBottomLapReferenceKind.Value;
                    break;
            }

            return (topDeltaBarKind, bottomDeltaBarKind);
        }

        private void RefreshReferenceLaps(LapPortionTimesComparatorViewModel lapPortionTimesComparatorViewModel, LapReferenceKind lapReferenceKind)
        {
            ILapInfo referenceLap;
            string title;

            if (_driver == null || string.IsNullOrWhiteSpace(_driver.CarClassId))
            {
                lapPortionTimesComparatorViewModel.ChangeReferencedLaps(null, null);
                return;
            }

            switch (lapReferenceKind)
            {
                case LapReferenceKind.None:
                    title = string.Empty;
                    lapPortionTimesComparatorViewModel.IsVisible = false;
                    return;
                case LapReferenceKind.SessionOverallBest:
                    title = "Δ to Session Best";
                    referenceLap = _driver.Session.GetBestLap();
                    break;
                case LapReferenceKind.SessionClassBest:
                    title = "Δ to Class Best";
                    referenceLap = _driver.Session.GetBestLap(_driver.CarClassId);
                    break;
                case LapReferenceKind.PlayerBest:
                    title = "Δ to Player Best";
                    referenceLap = _driver.BestLap;
                    break;
                case LapReferenceKind.LastLap:
                    title = "Δ to Previous Lap";
                    referenceLap = _driver.LastCompletedLap;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lapReferenceKind), lapReferenceKind, null);
            }

            lapPortionTimesComparatorViewModel.Title = title;

            if (referenceLap == null || _driver.CurrentLap == null)
            {
                lapPortionTimesComparatorViewModel.ClearComparedLap();
                return;
            }

            lapPortionTimesComparatorViewModel.IsVisible = true;
            lapPortionTimesComparatorViewModel.ChangeReferencedLaps(referenceLap, _driver.CurrentLap);
        }

        public void SetIsPaused(bool pauseState)
        {
            TopLapComparatorViewModel.IsPaused = pauseState;
            BottomLapComparatorViewModel.IsPaused = pauseState;
        }

        public void Dispose()
        {
            BottomLapComparatorViewModel?.Dispose();
            TopLapComparatorViewModel?.Dispose();
            _driver.NewLapStarted -= DriverOnLapCompletedOrInvalidated;
            _driver.LapCompleted -= DriverOnLapCompletedOrInvalidated;
            _driver.LapInvalidated -= DriverOnLapCompletedOrInvalidated;
        }

        private void DeltaLapSettingViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshReferenceLaps();
        }

        private void LapEventProviderOnBestClassLapChanged(object sender, BestLapChangedArgs e)
        {
            RefreshReferenceLaps();
        }

        private void DisplaySettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DisplaySettingsViewModel.AnimateDeltaTimes))
            {
                return;
            }

            TopLapComparatorViewModel.AnimateDeltaTimes = _displaySettingsViewModel.AnimateDeltaTimes;
            BottomLapComparatorViewModel.AnimateDeltaTimes = _displaySettingsViewModel.AnimateDeltaTimes;
        }
    }
}
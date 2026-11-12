namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System;
    using Contracts.Session;
    using Factory;
    using Model;

    public class PitEstimationSettingsViewModel : AbstractViewModel<PitEstimationSettings>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private PitEstimationVisualizationSettingsViewModel _alwaysEnabled;
        private PitEstimationVisualizationSettingsViewModel _pitRequested;
        private PitEstimationVisualizationSettingsViewModel _inPitWindow;
        private PitEstimationVisualizationSettingsViewModel _fuelLow;
        private int _lapsRemaining;
        private PitEstimationVisualizationSettingsViewModel _tyreWorn;
        private int _tyreWear;
        private double _extraTime;
        private bool _isOverridePitStallTimeEnabled;
        private double _overridePitStallTime;
        private bool _isOverridePitLaneTimeEnabled;
        private double _overridePitLaneTime;
        private bool _showPitBoardAtEntry;
        private PitEstimationVisualizationSettingsViewModel _stintEnding;

        public PitEstimationSettingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public PitEstimationVisualizationSettingsViewModel AlwaysEnabled
        {
            get => _alwaysEnabled;
            set => SetProperty(ref _alwaysEnabled, value);
        }

        public PitEstimationVisualizationSettingsViewModel PitRequested
        {
            get => _pitRequested;
            set => SetProperty(ref _pitRequested, value);
        }

        public PitEstimationVisualizationSettingsViewModel InPitWindow
        {
            get => _inPitWindow;
            set => SetProperty(ref _inPitWindow, value);
        }

        public PitEstimationVisualizationSettingsViewModel FuelLow
        {
            get => _fuelLow;
            set => SetProperty(ref _fuelLow, value);
        }

        public int LapsRemaining
        {
            get => _lapsRemaining;
            set => SetProperty(ref _lapsRemaining, value);
        }

        public PitEstimationVisualizationSettingsViewModel TyreWorn
        {
            get => _tyreWorn;
            set => SetProperty(ref _tyreWorn, value);
        }

        public PitEstimationVisualizationSettingsViewModel StintEnding
        {
            get => _stintEnding;
            set => SetProperty(ref _stintEnding, value);
        }

        public int TyreWear
        {
            get => _tyreWear;
            set => SetProperty(ref _tyreWear, value);
        }

        public double ExtraTime
        {
            get => _extraTime;
            set => SetProperty(ref _extraTime, value);
        }

        public bool IsOverridePitStallTimeEnabled
        {
            get => _isOverridePitStallTimeEnabled;
            set => SetProperty(ref _isOverridePitStallTimeEnabled, value);
        }

        public double OverridePitStallTime
        {
            get => _overridePitStallTime;
            set => SetProperty(ref _overridePitStallTime, value);
        }

        public bool IsOverridePitLaneTimeEnabled
        {
            get => _isOverridePitLaneTimeEnabled;
            set => SetProperty(ref _isOverridePitLaneTimeEnabled, value);
        }

        public double OverridePitLaneTime
        {
            get => _overridePitLaneTime;
            set => SetProperty(ref _overridePitLaneTime, value);
        }

        public bool ShowPitBoardAtEntry
        {
            get => _showPitBoardAtEntry;
            set => SetProperty(ref _showPitBoardAtEntry, value);
        }

        protected override void ApplyModel(PitEstimationSettings model)
        {
            AlwaysEnabled = _viewModelFactory.CreateAndApply<PitEstimationVisualizationSettingsViewModel, PitEstimationVisualizationSettings>(model.AlwaysEnabled);
            PitRequested = _viewModelFactory.CreateAndApply<PitEstimationVisualizationSettingsViewModel, PitEstimationVisualizationSettings>(model.PitRequested);
            InPitWindow = _viewModelFactory.CreateAndApply<PitEstimationVisualizationSettingsViewModel, PitEstimationVisualizationSettings>(model.InPitWindow);
            FuelLow = _viewModelFactory.CreateAndApply<PitEstimationVisualizationSettingsViewModel, PitEstimationVisualizationSettings>(model.FuelLow);
            TyreWorn = _viewModelFactory.CreateAndApply<PitEstimationVisualizationSettingsViewModel, PitEstimationVisualizationSettings>(model.TyreWorn);
            StintEnding = _viewModelFactory.CreateAndApply<PitEstimationVisualizationSettingsViewModel, PitEstimationVisualizationSettings>(model.StintEnding);

            LapsRemaining = model.LapsRemaining;
            TyreWear = model.TyreWear;
            ExtraTime = model.ExtraTime;
            IsOverridePitStallTimeEnabled = model.IsOverridePitStallTimeEnabled;
            OverridePitStallTime = model.OverridePitStallTime;
            IsOverridePitLaneTimeEnabled = model.IsOverridePitLaneTimeEnabled;
            OverridePitLaneTime = model.OverridePitLaneTime;
            ShowPitBoardAtEntry = model.ShowPitBoardAtEntry;
        }

        public override PitEstimationSettings SaveToNewModel()
        {
            return new PitEstimationSettings()
            {
                AlwaysEnabled = AlwaysEnabled.SaveToNewModel(),
                FuelLow = FuelLow.SaveToNewModel(),
                InPitWindow = InPitWindow.SaveToNewModel(),
                LapsRemaining = LapsRemaining,
                PitRequested = PitRequested.SaveToNewModel(),
                TyreWear = TyreWear,
                TyreWorn = TyreWorn.SaveToNewModel(),
                ExtraTime = ExtraTime,
                IsOverridePitStallTimeEnabled = IsOverridePitStallTimeEnabled,
                OverridePitStallTime = OverridePitStallTime,
                IsOverridePitLaneTimeEnabled = IsOverridePitLaneTimeEnabled,
                OverridePitLaneTime = OverridePitLaneTime,
                ShowPitBoardAtEntry = ShowPitBoardAtEntry,
                StintEnding = StintEnding.SaveToNewModel(),
            };
        }

        public bool IsReasonEnabled(PitPredictionReason reason, Func<PitEstimationVisualizationSettingsViewModel, bool> propertyValueExtractorFunc)
        {
            if (reason.HasFlag(PitPredictionReason.AlwaysEnabled) && propertyValueExtractorFunc(AlwaysEnabled))
            {
                return true;
            }

            if (reason.HasFlag(PitPredictionReason.FuelLevel) && propertyValueExtractorFunc(FuelLow))
            {
                return true;
            }

            if (reason.HasFlag(PitPredictionReason.PitRequested) && propertyValueExtractorFunc(PitRequested))
            {
                return true;
            }

            if (reason.HasFlag(PitPredictionReason.TyreStatus) && propertyValueExtractorFunc(TyreWorn))
            {
                return true;
            }

            if (reason.HasFlag(PitPredictionReason.StintEnding) && propertyValueExtractorFunc(StintEnding))
            {
                return true;
            }

            return reason.HasFlag(PitPredictionReason.PitWindow) && propertyValueExtractorFunc(InPitWindow);
        }
    }
}

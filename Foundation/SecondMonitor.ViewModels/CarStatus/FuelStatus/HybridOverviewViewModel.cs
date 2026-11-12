namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.FuelConsumption;

    public class HybridOverviewViewModel : AbstractViewModel<HybridConsumptionMonitor>
    {
        private double _currentHybridCharge;
        private double _previousLapHybridUsage;
        private double _currentLapHybridUsage;
        private double _lapsUntilEmptyOrFull;
        private HybridSystemMode _hybridSystemMode;
        private HybridChargeChange _hybridChargeChange;
        private string _lapToFullEmptyLabel;
        private string _lapsUntilEmptyFormatted;
        private bool _isVisible;
        private double _hybridAtRaceEnd;
        private HybridEnduranceState _hybridEnduranceState;

        public HybridOverviewViewModel(IViewModelFactory viewModelFactory)
        {
            CurrentHybridCharge = 76.24242;
            PreviousLapHybridUsage = -23.232;
            CurrentLapHybridUsage = 5.231;
            LapsUntilEmptyOrFull = 0;
            LapToFullEmptyLabel = CurrentLapHybridUsage < 0 ? "To Empty: " : "To Full: ";
            IsVisible = false;
            FuelAtRaceEndMarkerViewModel = viewModelFactory.Create<FuelAtRaceEndMarkerViewModel>();
            HybridAtRaceEnd = 20;
            FuelAtRaceEndMarkerViewModel.IsVisible = true;
            FuelAtRaceEndMarkerViewModel.FuelAtRaceEndPercentage = 20;
        }

        public FuelAtRaceEndMarkerViewModel FuelAtRaceEndMarkerViewModel { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public double CurrentHybridCharge
        {
            get => _currentHybridCharge;
            set => SetProperty(ref _currentHybridCharge, value);
        }

        public double PreviousLapHybridUsage
        {
            get => _previousLapHybridUsage;
            set => SetProperty(ref _previousLapHybridUsage, value);
        }

        public bool IsRecharging => HybridChargeChange == HybridChargeChange.Recharge;

        public bool IsDischarging => HybridChargeChange == HybridChargeChange.Discharge;

        public double CurrentLapHybridUsage
        {
            get => _currentLapHybridUsage;
            set => SetProperty(ref _currentLapHybridUsage, value);
        }

        public double LapsUntilEmptyOrFull
        {
            get => _lapsUntilEmptyOrFull;
            set => SetProperty(ref _lapsUntilEmptyOrFull, value);
        }

        public HybridSystemMode HybridSystemMode
        {
            get => _hybridSystemMode;
            set => SetProperty(ref _hybridSystemMode, value);
        }

        public HybridChargeChange HybridChargeChange
        {
            get => _hybridChargeChange;
            set => SetProperty(ref _hybridChargeChange, value, (_, __) =>
            {
                NotifyPropertyChanged(nameof(IsRecharging));
                NotifyPropertyChanged(nameof(IsDischarging));
            });
        }

        public string LapToFullEmptyLabel
        {
            get => _lapToFullEmptyLabel;
            set => SetProperty(ref _lapToFullEmptyLabel, value);
        }

        public double HybridAtRaceEnd
        {
            get => _hybridAtRaceEnd;
            set => SetProperty(ref _hybridAtRaceEnd, value);
        }

        public string LapsUntilEmptyFormatted
        {
            get => _lapsUntilEmptyFormatted;
            set => SetProperty(ref _lapsUntilEmptyFormatted, value);
        }

        public HybridEnduranceState HybridEnduranceState
        {
            get => _hybridEnduranceState;
            set => SetProperty(ref _hybridEnduranceState, value);
        }

        protected override void ApplyModel(HybridConsumptionMonitor model)
        {
            IsVisible = model.HybridSystemMode != HybridSystemMode.UnAvailable;
            CurrentHybridCharge = model.CurrentHybridCharge * 100;
            PreviousLapHybridUsage = model.PreviousLapHybridUsage * 100;
            CurrentLapHybridUsage = model.CurrentLapHybridUsage * 100;
            LapsUntilEmptyOrFull = model.LapsUntilEmptyOrFull;
            HybridSystemMode = model.HybridSystemMode;
            HybridChargeChange = model.HybridChargeChange;
            LapToFullEmptyLabel = PreviousLapHybridUsage <= 0 ? "To Empty: " : "To Full: ";
            LapsUntilEmptyFormatted = LapsUntilEmptyOrFull > 99 || double.IsNaN(LapsUntilEmptyOrFull) || double.IsInfinity(LapsUntilEmptyOrFull) ?
                "99+" : LapsUntilEmptyOrFull.ToString("F1");
            FuelAtRaceEndMarkerViewModel.FuelAtRaceEndPercentage = model.HybridChargeAtRaceEnd * 100;
            FuelAtRaceEndMarkerViewModel.IsVisible = model.IsStateAtRaceEndValid;
            HybridAtRaceEnd = model.HybridChargeAtRaceEnd * 100;
            HybridEnduranceState = model.HybridEnduranceState;
        }

        public override HybridConsumptionMonitor SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}

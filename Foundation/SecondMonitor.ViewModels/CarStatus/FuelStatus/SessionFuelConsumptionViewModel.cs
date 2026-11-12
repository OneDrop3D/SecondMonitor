namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using System.Windows.Input;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.FuelConsumption;
    using DataModel.Summary;
    using DataModel.Summary.FuelConsumption;
    using Settings;

    public class SessionFuelConsumptionViewModel : AbstractViewModel<SessionFuelConsumptionDto>, ISessionFuelConsumptionViewModel
    {
        private string _trackName;
        private Distance _lapDistance;
        private string _sessionType;
        private FuelConsumptionInfo _fuelConsumptionInfo;
        private Volume _avgPerMinute;
        private Volume _avgPerLap;
        private bool _isChecked;
        private bool _isCheckedVisible;
        private ICommand _deleteCommand;
        private bool _isDeleteButtonVisible;
        private bool _hasFuelCoef;
        private double _fuelCoef;

        public SessionFuelConsumptionViewModel(ISettingsProvider settingsProvider)
        {
            DistanceUnits = settingsProvider.DisplaySettingsViewModel.DistanceUnits;
            VolumeUnits = settingsProvider.DisplaySettingsViewModel.VolumeUnits;
            FuelPerDistanceUnits = settingsProvider.DisplaySettingsViewModel.FuelPerDistanceUnits;
        }

        public string TrackName
        {
            get => _trackName;
            set => SetProperty(ref _trackName, value);
        }

        public bool HasFuelCoef
        {
            get => _hasFuelCoef;
            set => SetProperty(ref _hasFuelCoef, value);
        }

        public double FuelCoef
        {
            get => _fuelCoef;
            set => SetProperty(ref _fuelCoef, value);
        }

        public Distance LapDistance
        {
            get => _lapDistance;
            set
            {
                SetProperty(ref _lapDistance, value);
                OnFuelConsumptionChanged();
            }
        }

        public DistanceUnits DistanceUnits { get; }

        public VolumeUnits VolumeUnits { get; }

        public string SessionType
        {
            get => _sessionType;
            set => SetProperty(ref _sessionType, value);
        }

        public FuelConsumptionInfo FuelConsumption
        {
            get => _fuelConsumptionInfo;
            set
            {
                SetProperty(ref _fuelConsumptionInfo, value);
                OnFuelConsumptionChanged();
            }
        }

        public Volume AvgPerMinute
        {
            get => _avgPerMinute;
            set => SetProperty(ref _avgPerMinute, value);
        }

        public Volume AvgPerLap
        {
            get => _avgPerLap;
            set => SetProperty(ref _avgPerLap, value);
        }

        public bool IsCheckedVisible
        {
            get => _isCheckedVisible;
            set => SetProperty(ref _isCheckedVisible, value);
        }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public FuelPerDistanceUnits FuelPerDistanceUnits
        {
            get;
        }

        public ICommand DeleteCommand
        {
            get => _deleteCommand;
            set => SetProperty(ref _deleteCommand, value);
        }

        public bool IsDeleteButtonVisible
        {
            get => _isDeleteButtonVisible;
            set => SetProperty(ref _isDeleteButtonVisible, value);
        }

        protected override void ApplyModel(SessionFuelConsumptionDto model)
        {
            TrackName = model.TrackFullName;
            LapDistance = Distance.FromMeters(model.LapDistance);
            FuelConsumption = new FuelConsumptionInfo(model.ConsumedFuel, TimeSpan.FromSeconds(model.ElapsedSeconds), Distance.FromMeters(model.TraveledDistanceMeters));
            SessionType = model.IsWetSession ? model.SessionKind + "(WET)" : model.SessionKind.ToString();
            HasFuelCoef = model.HasVirtualEnergyFuelCoef;
            FuelCoef = model.VirtualEnergyFuelCoef;
        }

        public override SessionFuelConsumptionDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        private void OnFuelConsumptionChanged()
        {
            if (FuelConsumption == null || LapDistance == null)
            {
                return;
            }

            AvgPerMinute = FuelConsumption.GetAveragePerMinute();
            AvgPerLap = FuelConsumption.GetAveragePerDistance(LapDistance);
        }
    }
}
namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Summary;
    using Settings;
    using Settings.ViewModel;

    public class FuelCalculatorViewModel : AbstractViewModel
    {
        private FuelConsumptionInfo _fuelConsumption;
        private int _requiredLaps;
        private int _requiredMinutes;
        private double _lapsDistance;
        private int _extraFuel;
        private Volume _requiredFuel;
        private bool _isFuelCalculationSuppressed;
        private int _fuelMultiplier;

        public FuelCalculatorViewModel(ISettingsProvider settingsProvider)
        {
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            FuelMultiplier = 1;
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public FuelConsumptionInfo FuelConsumption
        {
            get => _fuelConsumption;
            set
            {
                SetProperty(ref _fuelConsumption, value);
                CalculateRequiredFuel();
            }
        }

        public int RequiredLaps
        {
            get => _requiredLaps;
            set
            {
                SetProperty(ref _requiredLaps, value);
                CalculateRequiredFuel();
            }
        }

        public int RequiredMinutes
        {
            get => _requiredMinutes;
            set
            {
                SetProperty(ref _requiredMinutes, value);
                CalculateRequiredFuel();
            }
        }

        public double LapDistance
        {
            get => _lapsDistance;
            set
            {
                SetProperty(ref _lapsDistance, value);
                CalculateRequiredFuel();
            }
        }

        public int ExtraFuel
        {
            get => _extraFuel;
            set
            {
                SetProperty(ref _extraFuel, value);
                CalculateRequiredFuel();
            }
        }

        public Volume RequiredFuel
        {
            get => _requiredFuel;
            set => SetProperty(ref _requiredFuel, value);
        }

        public bool UpdateDefaultSettingsOnValueChanged { get; set; }
        public int FuelMultiplier
        {
            get => _fuelMultiplier;
            set
            {
                _fuelMultiplier = value;
                CalculateRequiredFuel();
            }
        }

        public void SetRequiredRunTime(int laps, int minutes, int extraFuel)
        {
            _isFuelCalculationSuppressed = true;
            RequiredLaps = laps;
            RequiredMinutes = minutes;
            ExtraFuel = extraFuel;
            NotifyPropertyChanged(string.Empty);
            _isFuelCalculationSuppressed = false;
            CalculateRequiredFuel();
        }

        private void CalculateRequiredFuel()
        {
            if (_isFuelCalculationSuppressed)
            {
                return;
            }

            if (FuelConsumption == null)
            {
                RequiredFuel = Volume.FromLiters(0);
                return;
            }

            if (UpdateDefaultSettingsOnValueChanged)
            {
                DisplaySettingsViewModel.DefaultRaceLaps = _requiredLaps;
                DisplaySettingsViewModel.DefaultRaceMinutes = _requiredMinutes;
            }

            double coefficient = 1 + (_extraFuel / 100.0);

            RequiredFuelCalculator calculator = new RequiredFuelCalculator(FuelConsumption);
            RequiredFuel = calculator.GetRequiredFuel(TimeSpan.FromMinutes(RequiredMinutes),
                Distance.FromMeters(LapDistance * RequiredLaps), coefficient) * FuelMultiplier;
        }
    }
}

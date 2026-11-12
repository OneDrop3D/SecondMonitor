namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.Stint
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Settings;

    public class StintPitStopViewModel(ISettingsProvider settingsProvider) : StandardPitStopViewModel(settingsProvider)
    {
        private string _currentStintTime;
        private Volume _fuelToAddThisLap;
        private bool _isFuelToAddThisLapVisible;
        private bool _isFuelToAddPitLapVisible;
        private string _nextStintTime;
        private string _lastStintTime;
        private bool _isStintTimeVisible;

        public string CurrentStintTime
        {
            get => _currentStintTime;
            set => SetProperty(ref _currentStintTime, value);
        }

        public string NextStintTime
        {
            get => _nextStintTime;
            set => SetProperty(ref _nextStintTime, value);
        }

        public string LastStintTime
        {
            get => _lastStintTime;
            set => SetProperty(ref _lastStintTime, value);
        }

        public Volume FuelToAddThisLap
        {
            get => _fuelToAddThisLap;
            set => SetProperty(ref _fuelToAddThisLap, value);
        }

        public bool IsFuelToAddThisLapVisible
        {
            get => _isFuelToAddThisLapVisible;
            set => SetProperty(ref _isFuelToAddThisLapVisible, value);
        }

        public bool IsFuelToAddPitLapVisible
        {
            get => _isFuelToAddPitLapVisible;
            set => SetProperty(ref _isFuelToAddPitLapVisible, value);
        }

        public bool IsStintTimeVisible
        {
            get => _isStintTimeVisible;
            set => SetProperty(ref _isStintTimeVisible, value);
        }

        public override Volume GetFuelToAddNow(IFuelOverviewViewModel fuelOverviewViewModel)
        {
            return IsFuelToAddThisLapVisible ? FuelToAddThisLap : FuelForNextPitStop;
        }
    }
}

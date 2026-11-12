namespace SecondMonitor.ViewModels.CarStatus
{
    using FuelStatus;
    using Settings;

    public class WheelStatusViewModelFactory
    {
        private readonly ISettingsProvider _settingsProvider;

        public WheelStatusViewModelFactory(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public WheelStatusViewModel Create(bool isLeftWheel, SessionRemainingCalculator sessionRemainingCalculator, IPaceProvider paceProvider, IFuelPredictionProvider fuelPredictionProvider)
        {
            return new WheelStatusViewModel(isLeftWheel, sessionRemainingCalculator, _settingsProvider, paceProvider, fuelPredictionProvider);
        }

        public WheelStatusViewModel Create(bool isLeftWheel)
        {
            return new WheelStatusViewModel(isLeftWheel, _settingsProvider);
        }
    }
}
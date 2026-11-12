namespace SecondMonitor.ViewModels.CarStatus
{
    using FuelStatus;
    using Settings.ViewModel;

    public class CarAndFuelStatusViewModel : AbstractViewModel
    {
        private CarStatusViewModel _carStatusViewModel;
        private FuelOverviewViewModel _fuelOverviewViewModel;
        private DisplaySettingsViewModel _displaySettingsViewModel;

        public CarStatusViewModel CarStatusViewModel
        {
            get => _carStatusViewModel;
            set => SetProperty(ref _carStatusViewModel, value);
        }

        public FuelOverviewViewModel FuelOverviewViewModel
        {
            get => _fuelOverviewViewModel;
            set => SetProperty(ref _fuelOverviewViewModel, value);
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel
        {
            get => _displaySettingsViewModel;
            set => SetProperty(ref _displaySettingsViewModel, value);
        }
    }
}
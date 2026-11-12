namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class FuelToAddMarkerViewModel : AbstractViewModel, IFuelMarkerViewModel
    {
        private bool _isVisible;
        private double _bottomMarginPercentage;
        private double _heightPercentage;
        private Volume _afterRefuelFuel;

        public FuelToAddMarkerViewModel(DisplaySettingsViewModel displaySettingsViewModel)
        {
            DisplaySettingsViewModel = displaySettingsViewModel;
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public double BottomMarginPercentage
        {
            get => _bottomMarginPercentage;
            set => SetProperty(ref _bottomMarginPercentage, value);
        }

        public double HeightPercentage
        {
            get => _heightPercentage;
            set => SetProperty(ref _heightPercentage, value);
        }

        public Volume AfterRefuelFuel
        {
            get => _afterRefuelFuel;
            set => SetProperty(ref _afterRefuelFuel, value);
        }

        public void Update(IFuelOverviewViewModel fuelOverviewViewModel)
        {
            if (fuelOverviewViewModel.PitStopsViewModel?.IsFuelToAddVisible == true && fuelOverviewViewModel.PitStopsViewModel.RefuelingWindowState != RefuelingWindowState.Closed)
            {
                IsVisible = true;
                BottomMarginPercentage = fuelOverviewViewModel.FuelPercentage;
                double fuelToAddInLiters = fuelOverviewViewModel.PitStopsViewModel.GetFuelToAddNow(fuelOverviewViewModel).InLiters;
                double currentFuelInLiters = fuelOverviewViewModel.FuelPercentage / 100.0 * fuelOverviewViewModel.MaximumFuel.InLiters;

                if (fuelToAddInLiters + currentFuelInLiters >= fuelOverviewViewModel.MaximumFuel.InLiters)
                {
                    fuelToAddInLiters = fuelOverviewViewModel.MaximumFuel.InLiters - currentFuelInLiters;
                }

                AfterRefuelFuel = Volume.FromLiters(currentFuelInLiters + fuelToAddInLiters);

                HeightPercentage = (fuelToAddInLiters / fuelOverviewViewModel.MaximumFuel.InLiters) * 100.0;
            }
            else
            {
                IsVisible = false;
                BottomMarginPercentage = 0;
                HeightPercentage = 0;
            }
        }
    }
}

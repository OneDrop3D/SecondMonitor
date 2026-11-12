namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;

    public class FuelAtRaceEndMarkerViewModel : AbstractViewModel, IFuelMarkerViewModel
    {
        private bool _isVisible;
        private double _fuelAtRaceEndPercentage;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public double FuelAtRaceEndPercentage
        {
            get => _fuelAtRaceEndPercentage;
            set => SetProperty(ref _fuelAtRaceEndPercentage, value);
        }

        public void Update(IFuelOverviewViewModel fuelOverviewViewModel)
        {
            if (fuelOverviewViewModel.FuelDelta.InLiters > 0)
            {
                IsVisible = true;
                FuelAtRaceEndPercentage = Math.Min((fuelOverviewViewModel.FuelDelta.InLiters / fuelOverviewViewModel.MaximumFuel.InLiters) * 100, 100);
            }
            else
            {
                IsVisible = false;
                FuelAtRaceEndPercentage = 0;
            }
        }
    }
}

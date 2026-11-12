namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.VirtualTankToTank
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Settings;

    public class VirtualTankToTankPitStopViewModel : StandardPitStopViewModel
    {
        private string _virtualEnergyNextPitStop;
        private string _fuelCoefNextPitStop;
        private string _virtualEnergyLastPitStop;
        private bool _isFuelForNextPitStopTotal1;

        public VirtualTankToTankPitStopViewModel(ISettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        public string VirtualEnergyNextPitStop
        {
            get => _virtualEnergyNextPitStop;
            set => SetProperty(ref _virtualEnergyNextPitStop, value);
        }

        public string FuelCoefNextPitStop
        {
            get => _fuelCoefNextPitStop;
            set => SetProperty(ref _fuelCoefNextPitStop, value);
        }

        public string VirtualEnergyLastPitStop
        {
            get => _virtualEnergyLastPitStop;
            set => SetProperty(ref _virtualEnergyLastPitStop, value);
        }

        public bool IsFuelForNextPitStopTotal
        {
            get => _isFuelForNextPitStopTotal1;
            set => SetProperty(ref _isFuelForNextPitStopTotal1, value);
        }

        public override Volume GetFuelToAddNow(IFuelOverviewViewModel fuelOverviewViewModel)
        {
            if (!IsFuelForNextPitStopTotal)
            {
                return FuelForNextPitStop;
            }

            double currentFuelInLiters = fuelOverviewViewModel.FuelPercentage / 100.0 * fuelOverviewViewModel.MaximumFuel.InLiters;
            return Volume.FromLiters(FuelForNextPitStop.InLiters - currentFuelInLiters);
        }
    }
}

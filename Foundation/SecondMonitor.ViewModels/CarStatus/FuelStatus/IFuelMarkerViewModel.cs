namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    public interface IFuelMarkerViewModel : IViewModel
    {
        void Update(IFuelOverviewViewModel fuelOverviewViewModel);
    }
}
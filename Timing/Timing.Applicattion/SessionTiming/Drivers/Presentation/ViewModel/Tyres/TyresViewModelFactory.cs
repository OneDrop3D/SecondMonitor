namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.Tyres
{
    using System.Linq;

    using SecondMonitor.Timing.Common.SessionTiming.Drivers;

    public class TyresViewModelFactory
    {
        public ITyresViewModel CreateDefault()
        {
            return new SingleCompoundViewModel();
        }

        public ITyresViewModel Create(DriverTiming driverTiming)
        {
            return driverTiming.DriverInfo.CarInfo.WheelsInfo.AllWheels.All(x => x.TyreVisualType == driverTiming.DriverInfo.CarInfo.WheelsInfo.FrontLeft.TyreVisualType) ?
                new SingleCompoundViewModel() : new MixedCompoundsViewModel();
        }
    }
}

namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.Tyres
{
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.ViewModels;

    public interface ITyresViewModel : IViewModel<DriverTiming>
    {
        bool CanBeUSed(DriverTiming driverTiming);

        bool HasValue { get; }
    }
}

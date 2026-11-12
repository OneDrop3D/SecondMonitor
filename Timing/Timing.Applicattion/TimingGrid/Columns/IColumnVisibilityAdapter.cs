namespace SecondMonitor.Timing.Application.TimingGrid.Columns
{
    using System.Collections.Generic;
    using SessionTiming.Drivers.Presentation.ViewModel;

    public interface IColumnVisibilityAdapter
    {
        bool IsColumnVisible(IEnumerable<DriverTimingViewModel> driverTimings);
    }
}
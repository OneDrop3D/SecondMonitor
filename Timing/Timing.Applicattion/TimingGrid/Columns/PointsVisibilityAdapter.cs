namespace SecondMonitor.Timing.Application.TimingGrid.Columns
{
    using System.Collections.Generic;
    using System.Linq;
    using SessionTiming.Drivers.Presentation.ViewModel;

    public class PointsVisibilityAdapter : IColumnVisibilityAdapter
    {
        public bool IsColumnVisible(IEnumerable<DriverTimingViewModel> driverTimings)
        {
            return driverTimings.Any(x => !x.IsPlayer && x.DriverTiming.ChampionshipPoints > 0);
        }
    }
}
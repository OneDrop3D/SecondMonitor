namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Ordering
{
    using System.Collections.Generic;
    using System.Linq;
    using ViewModels.Settings.Model;

    public class RelativeDriversOrdering : IDriversOrdering
    {
        public string Name => nameof(DriverOrderKind.Relative);
        public List<DriverTiming> Order(ICollection<DriverTiming> driverTimings)
        {
            return driverTimings.OrderBy(x => x.DistanceToPlayer).ToList();
        }
    }
}
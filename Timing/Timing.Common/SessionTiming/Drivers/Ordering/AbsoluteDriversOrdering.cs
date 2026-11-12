namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Ordering
{
    using System.Collections.Generic;
    using System.Linq;
    using ViewModels.Settings.Model;

    public class AbsoluteDriversOrdering : IDriversOrdering
    {
        public string Name => nameof(DriverOrderKind.Absolute);
        public List<DriverTiming> Order(ICollection<DriverTiming> driverTimings)
        {
            return driverTimings.OrderBy(x => x.Position).ToList();
        }
    }
}
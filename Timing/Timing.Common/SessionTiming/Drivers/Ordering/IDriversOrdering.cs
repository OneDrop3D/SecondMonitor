namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Ordering
{
    using System.Collections.Generic;

    public interface IDriversOrdering
    {
        string Name { get; }

        List<DriverTiming> Order(ICollection<DriverTiming> driverTimings);
    }
}
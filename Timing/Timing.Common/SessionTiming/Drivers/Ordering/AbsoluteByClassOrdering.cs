namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Ordering
{
    using System.Collections.Generic;
    using System.Linq;

    public class AbsoluteByClassOrdering : IDriversOrdering
    {
        public string Name { get; set; }
        public List<DriverTiming> Order(ICollection<DriverTiming> driverTimings)
        {
            return driverTimings.GroupBy(x => x.CarClassId)
                .Select(group => new { Name = group.Key, Drivers = group.OrderBy(x => x.Position) })
                .OrderBy(x => x.Drivers.First().Position).SelectMany(x => x.Drivers).ToList();
        }
    }
}
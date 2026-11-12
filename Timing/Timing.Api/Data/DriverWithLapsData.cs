namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;

    public class DriverWithLapsData : DriverData
    {
        public DriverWithLapsData(SessionIdentification sessionIdentification, DriverTiming driverTiming) : base(sessionIdentification, driverTiming)
        {
            Laps = driverTiming.Laps.Select(x => new LapData(x)).ToList();
        }

        public IReadOnlyCollection<LapData> Laps { get; }
    }
}

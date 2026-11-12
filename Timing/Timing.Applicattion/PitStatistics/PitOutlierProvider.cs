namespace SecondMonitor.Timing.Application.PitStatistics
{
    using SecondMonitor.Contracts.Statistics;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.PitStop;

    public class PitOutlierProvider : OutliersProviderBase<PitStopInfo>
    {
        protected override double GetValue(PitStopInfo element)
        {
            return element.PitStopStallDuration.TotalSeconds;
        }
    }
}

namespace SecondMonitor.Timing.Application.PitStatistics
{
    using System.Collections.Generic;

    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.PitStop;

    public interface IPitOutlierFilter
    {
        IList<PitStopInfo> FilterOutOutliers(IList<PitStopInfo> pitStopInfos);
    }
}

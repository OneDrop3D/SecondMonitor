namespace SecondMonitor.Timing.Application.PitStatistics
{
    using System.Collections.Generic;
    using System.Linq;

    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.PitStop;

    public class PitOutlierFilter : IPitOutlierFilter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly PitOutlierProvider _pitOutlierProvider;

        public PitOutlierFilter(PitOutlierProvider pitOutlierProvider)
        {
            _pitOutlierProvider = pitOutlierProvider;
        }

        public IList<PitStopInfo> FilterOutOutliers(IList<PitStopInfo> pitStopInfos)
        {
            IList<PitStopInfo> outliers = _pitOutlierProvider.GetOutliers(pitStopInfos);
            outliers.ForEach(x => logger.Info($"Pit Stop Outlier Detected, Pit Stall Time: {x.PitStopStallDuration.TotalSeconds:F2}"));

            return pitStopInfos.Except(outliers).ToList();
        }
    }
}

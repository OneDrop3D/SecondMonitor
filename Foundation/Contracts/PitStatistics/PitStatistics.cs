namespace SecondMonitor.Contracts.PitStatistics
{
    using System;

    public class PitStatistics
    {
        public PitStatistics(TimeSpan totalPitDuration, TimeSpan pitStallDuration, TimeSpan pitLaneDuration, TimeSpan totalTimeLost, int entriesCount)
        {
            TotalPitDuration = totalPitDuration;
            PitStallDuration = pitStallDuration;
            PitLaneDuration = pitLaneDuration;
            TotalTimeLost = totalTimeLost;
            EntriesCount = entriesCount;
        }

        public TimeSpan TotalPitDuration { get; }

        public TimeSpan PitStallDuration { get; }

        public TimeSpan PitLaneDuration { get; }
        public TimeSpan TotalTimeLost { get; }

        public int EntriesCount { get; }
    }
}
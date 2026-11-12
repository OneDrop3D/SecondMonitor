namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System;

    public class DriverGapInformation
    {
        public DriverGapInformation()
        {
        }

        public DriverGapInformation(TimeSpan gap, int lapDifference)
        {
            Gap = gap;
            LapDifference = lapDifference;
        }

        public TimeSpan Gap { get; set; }

        public int LapDifference { get; set; }
    }
}

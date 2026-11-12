namespace SecondMonitor.Timing.Common.PitStatistics
{
    using System;
    using SessionTiming.Drivers.Lap.PitStop;

    public class PitStopArgs : EventArgs
    {
        public PitStopArgs(PitStopInfo pitStop)
        {
            PitStop = pitStop;
        }

        public PitStopInfo PitStop { get; }
    }
}
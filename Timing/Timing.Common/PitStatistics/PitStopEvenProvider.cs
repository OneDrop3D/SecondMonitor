namespace SecondMonitor.Timing.Common.PitStatistics
{
    using System;
    using SessionTiming.Drivers;
    using SessionTiming.Drivers.Lap.PitStop;

    public class PitStopEvenProvider : IPitStopEvenProvider
    {
        public event EventHandler<PitStopArgs> PitStopCompleted;

        public void NotifyPitStopCompleted(DriverTiming driverTiming, PitStopInfo pitStop)
        {
            PitStopCompleted?.Invoke(driverTiming, new PitStopArgs(pitStop));
        }
    }
}
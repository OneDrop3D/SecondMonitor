namespace SecondMonitor.Timing.Common.PitStatistics
{
    using System;
    using SessionTiming.Drivers;
    using SessionTiming.Drivers.Lap.PitStop;

    public interface IPitStopEvenProvider
    {
        event EventHandler<PitStopArgs> PitStopCompleted;

        void NotifyPitStopCompleted(DriverTiming driverTiming, PitStopInfo pitStop);
    }
}
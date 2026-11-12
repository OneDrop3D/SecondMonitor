namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    public enum LapInvalidationReasonKind
    {
        None,
        NoValidLapTime,
        CompletedDistanceLessThanLapThreshold,
        NotAllSectorHaveTime,
        SanityMaxSpeedViolated,
        InvalidatedBySectorTime,
        InvalidatedFirstLap,
        DriverDnf,
        DriverInPits,
        InvalidatedBySim
    }
}
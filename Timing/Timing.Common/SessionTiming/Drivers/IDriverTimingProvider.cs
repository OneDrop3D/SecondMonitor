namespace SecondMonitor.Timing.Common.SessionTiming.Drivers
{
    using System.Collections.Generic;

    public interface IDriverTimingProvider
    {
        bool TryGetTiming(string driverSessionId, out DriverTiming driverTiming);

        bool TryGetPlayerTiming(out DriverTiming driverTiming);

        IEnumerable<DriverTiming> GetAll();
    }
}
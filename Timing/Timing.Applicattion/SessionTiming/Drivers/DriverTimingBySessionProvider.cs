namespace SecondMonitor.Timing.Application.SessionTiming.Drivers
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.SessionTiming.Drivers;
    using SessionTiming.ViewModel;

    public class DriverTimingBySessionProvider : IDriverTimingProvider
    {
        private SessionTimingController _sessionTiming;

        public void RegisterSessionTimingController(SessionTimingController sessionTimingController)
        {
            _sessionTiming = sessionTimingController;
        }

        public bool TryGetTiming(string driverSessionId, out DriverTiming driverTiming)
        {
            if (_sessionTiming == null)
            {
                driverTiming = null;
                return false;
            }

            return _sessionTiming.Drivers.TryGetValue(driverSessionId, out driverTiming);
        }

        public bool TryGetPlayerTiming(out DriverTiming driverTiming)
        {
            driverTiming = _sessionTiming?.Player;
            return driverTiming != null;
        }

        public IEnumerable<DriverTiming> GetAll()
        {
            return _sessionTiming?.Drivers.Values ?? Enumerable.Empty<DriverTiming>();
        }
    }
}
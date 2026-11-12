namespace SecondMonitor.Contracts.Session
{
    using System;

    using DataModel.Snapshot.Drivers;

    public class SessionInformationProviderNullObject : ISessionInformationProvider
    {
        public bool IsDriverOnValidLap(IDriverInfo driver)
        {
            return true;
        }

        public bool IsDriverLastSectorGreen(IDriverInfo driver, int sectorNumber)
        {
            return false;
        }

        public bool IsDriverLastSectorPurple(IDriverInfo driver, int sectorNumber)
        {
            return false;
        }

        public bool IsDriverLastClassSectorPurple(IDriverInfo driver, int sectorNumber)
        {
            return false;
        }

        public bool HasDriverCompletedPitWindowStop(IDriverInfo driverInfo)
        {
            return false;
        }

        public int? GetPlayerPitStopCount()
        {
            return 0;
        }

        public int GetDriverPitStopCount(IDriverInfo driverInfo)
        {
            return 0;
        }

        public TimeSpan GetDriverCurrentLapTime(IDriverInfo driverInfo)
        {
            return TimeSpan.Zero;
        }
    }
}
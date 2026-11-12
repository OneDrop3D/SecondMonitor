namespace SecondMonitor.Contracts.Session
{
    using System;

    using DataModel.Snapshot.Drivers;

    public interface ISessionInformationProvider
    {
        bool IsDriverOnValidLap(IDriverInfo driver);
        bool IsDriverLastSectorGreen(IDriverInfo driver, int sectorNumber);
        bool IsDriverLastSectorPurple(IDriverInfo driver, int sectorNumber);
        bool IsDriverLastClassSectorPurple(IDriverInfo driver, int sectorNumber);
        bool HasDriverCompletedPitWindowStop(IDriverInfo driverInfo);
        int? GetPlayerPitStopCount();
        int GetDriverPitStopCount(IDriverInfo driverInfo);
        TimeSpan GetDriverCurrentLapTime(IDriverInfo driverInfo);
    }
}
namespace SecondMonitor.Timing.Common.SessionTiming
{
    using System;
    using Drivers.Lap;

    using SecondMonitor.Contracts.Session;

    public interface ILapEventProvider : ILapCompletedEventProvider
    {
        event EventHandler<BestLapChangedArgs> BestLapPersonalChanged;
        event EventHandler<BestLapChangedArgs> BestLapChanged;
        event EventHandler<BestLapChangedArgs> BestClassLapChanged;
        event EventHandler<BestSectorChangedArgs> BestSectorChanged;
        event EventHandler<BestSectorChangedArgs> BestSectorClassChanged;
        event EventHandler<LapEventArgs> LapCompleted;

        void NotifyBestLapChanged(object sender, ILapInfo oldLap, ILapInfo newLap);
        void NotifyBestClassLapChanged(object sender, ILapInfo oldLap, ILapInfo newLap);
        void NotifyBestLapPersonalChanged(object sender, ILapInfo oldLap, ILapInfo newLap);

        void NotifyBestSectorChanged(object sender, SectorTiming oldSector, SectorTiming newSector);
        void NotifyBestSectorClassChanged(object sender, SectorTiming oldSector, SectorTiming newSector);

        void NotifyLapCompleted(object sender, ILapInfo lap);
    }
}
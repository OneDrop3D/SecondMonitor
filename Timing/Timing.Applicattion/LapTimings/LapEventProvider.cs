namespace SecondMonitor.Timing.Application.LapTimings
{
    using System;
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers.Lap;

    using SecondMonitor.Contracts.Session;

    public class LapEventProvider : ILapEventProvider
    {
        public event EventHandler<BestLapChangedArgs> BestLapPersonalChanged;
        public event EventHandler<BestLapChangedArgs> BestLapChanged;
        public event EventHandler<BestLapChangedArgs> BestClassLapChanged;
        public event EventHandler<BestSectorChangedArgs> BestSectorChanged;
        public event EventHandler<BestSectorChangedArgs> BestSectorClassChanged;
        public event EventHandler<LapEventArgs> LapCompleted;
        public event EventHandler<LapSummaryArgs> LapCompletedSummary;

        public void NotifyBestLapChanged(object sender, ILapInfo oldLap, ILapInfo newLap)
        {
            BestLapChanged?.Invoke(sender, new BestLapChangedArgs(oldLap, newLap));
        }

        public void NotifyBestClassLapChanged(object sender, ILapInfo oldLap, ILapInfo newLap)
        {
            BestClassLapChanged?.Invoke(sender, new BestLapChangedArgs(oldLap, newLap));
        }

        public void NotifyBestLapPersonalChanged(object sender, ILapInfo oldLap, ILapInfo newLap)
        {
            BestLapPersonalChanged?.Invoke(sender, new BestLapChangedArgs(oldLap, newLap));
        }

        public void NotifyBestSectorChanged(object sender, SectorTiming oldSector, SectorTiming newSector)
        {
            BestSectorChanged?.Invoke(sender, new BestSectorChangedArgs(oldSector, newSector));
        }

        public void NotifyBestSectorClassChanged(object sender, SectorTiming oldSector, SectorTiming newSector)
        {
            BestSectorClassChanged?.Invoke(sender, new BestSectorChangedArgs(oldSector, newSector));
        }

        public void NotifyLapCompleted(object sender, ILapInfo lap)
        {
            LapCompleted?.Invoke(sender, new LapEventArgs(lap));
            LapCompletedSummary?.Invoke(sender, new LapSummaryArgs(lap.Driver.DriverInfo, lap.LapNumber, lap.LapTime));
        }
    }
}
namespace SecondMonitor.Timing.Common.SessionTiming
{
    using Drivers.Lap;

    public class BestSectorChangedArgs
    {
        public BestSectorChangedArgs(SectorTiming oldSector, SectorTiming newSector)
        {
            OldSector = oldSector;
            NewSector = newSector;
        }

        public SectorTiming OldSector { get; }
        public SectorTiming NewSector { get; }
    }
}
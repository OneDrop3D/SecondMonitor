namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;

    public class SectorCompletedArgs : EventArgs
    {
        public SectorCompletedArgs(SectorTiming sectorTiming)
        {
            SectorTiming = sectorTiming;
        }

        public SectorTiming SectorTiming { get; }
    }
}
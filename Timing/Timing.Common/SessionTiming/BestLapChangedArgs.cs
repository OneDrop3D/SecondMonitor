namespace SecondMonitor.Timing.Common.SessionTiming
{
    using System;
    using Drivers.Lap;

    public class BestLapChangedArgs : EventArgs
    {
        public BestLapChangedArgs(ILapInfo oldLap, ILapInfo newLap)
        {
            OldLap = oldLap;
            NewLap = newLap;
        }

        public ILapInfo OldLap { get; }
        public ILapInfo NewLap { get; }
    }
}
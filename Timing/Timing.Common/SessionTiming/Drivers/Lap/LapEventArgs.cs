namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;

    public class LapEventArgs : EventArgs
    {
        public LapEventArgs(ILapInfo lapInfo)
        {
            Lap = lapInfo;
        }

        public ILapInfo Lap
        {
            get;
        }
    }
}
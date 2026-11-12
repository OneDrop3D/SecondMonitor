namespace SecondMonitor.Timing.Application.SessionTiming.Drivers
{
    using System;
    using Common.SessionTiming.Drivers;

    public class DriverListModificationEventArgs : EventArgs
    {
        public DriverListModificationEventArgs(DriverTiming data)
        {
            Data = data;
        }

        public DriverTiming Data
        {
            get;
            set;
        }
    }
}
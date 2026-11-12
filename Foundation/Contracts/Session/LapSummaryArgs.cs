namespace SecondMonitor.Contracts.Session
{
    using System;

    using SecondMonitor.DataModel.Snapshot.Drivers;

    public class LapSummaryArgs : EventArgs
    {
        public LapSummaryArgs(IDriverInfo driverInfo, int lapNumber, TimeSpan lapTime)
        {
            DriverInfo = driverInfo;
            LapNumber = lapNumber;
            LapTime = lapTime;
        }

        public IDriverInfo DriverInfo { get; }

        public int LapNumber { get; }

        public TimeSpan LapTime { get; }
    }
}

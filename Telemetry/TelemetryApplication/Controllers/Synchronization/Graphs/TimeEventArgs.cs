namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization.Graphs
{
    using System;

    public class TimeEventArgs : EventArgs
    {
        public TimeEventArgs(TimeSpan time, DistanceRequestKind distanceRequestKind)
        {
            Time = time;
            DistanceRequestKind = distanceRequestKind;
        }

        public TimeSpan Time { get; }

        public DistanceRequestKind DistanceRequestKind { get; }
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization.Graphs
{
    using System;

    public class DistanceEventArgs : EventArgs
    {
        public DistanceEventArgs(double distance, DistanceRequestKind distanceRequestKind)
        {
            Distance = distance;
            DistanceRequestKind = distanceRequestKind;
        }

        public double Distance { get; }

        public DistanceRequestKind DistanceRequestKind { get; }
    }
}
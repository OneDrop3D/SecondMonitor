namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization.Graphs
{
    using System;

    public class ScaleEventArgs : EventArgs
    {
        public ScaleEventArgs(double newScale)
        {
            NewScale = newScale;
        }

        public double NewScale { get; }
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization.Graphs
{
    using System;

    public class BusyStateEventArgs : EventArgs
    {
        public BusyStateEventArgs(bool isBusy)
        {
            IsBusy = isBusy;
        }

        public bool IsBusy { get; }
    }
}
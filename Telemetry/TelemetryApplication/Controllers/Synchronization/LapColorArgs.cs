namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using DataModel.BasicProperties;

    public class LapColorArgs : EventArgs
    {
        public LapColorArgs(string lapId, ColorDto color)
        {
            LapId = lapId;
            Color = color;
        }

        public string LapId { get; set; }
        public ColorDto Color { get; set; }
    }
}
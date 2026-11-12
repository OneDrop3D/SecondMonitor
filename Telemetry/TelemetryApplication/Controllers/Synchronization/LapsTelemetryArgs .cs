namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using System.Collections.Generic;
    using TelemetryManagement.DTO;

    public class LapsTelemetryArgs : EventArgs
    {
        public LapsTelemetryArgs(IReadOnlyCollection<LapTelemetryDto> lapsTelemetries)
        {
            LapsTelemetries = lapsTelemetries;
        }

        public IReadOnlyCollection<LapTelemetryDto> LapsTelemetries { get; }
    }
}
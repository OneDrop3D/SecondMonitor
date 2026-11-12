namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using System.Collections.Generic;
    using TelemetryManagement.DTO;

    public class LapsSummaryArgs : EventArgs
    {
        public LapsSummaryArgs(IReadOnlyCollection<LapSummaryDto> lapsSummaries)
        {
            LapsSummaries = lapsSummaries;
        }

        public IReadOnlyCollection<LapSummaryDto> LapsSummaries { get; }
    }
}
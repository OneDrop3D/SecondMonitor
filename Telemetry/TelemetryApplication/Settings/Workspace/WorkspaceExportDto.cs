namespace SecondMonitor.Telemetry.TelemetryApplication.Settings.Workspace
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class WorkspaceExportDto
    {
        public WorkspaceExportDto()
        {
            ChartSets = new List<ChartSet>();
        }

        public Workspace Workspace { get; set; }
        public List<ChartSet> ChartSets { get; set; }
    }
}
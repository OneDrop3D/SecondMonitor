namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers
{
    using System;
    using System.Collections.Generic;
    using Settings.Workspace;

    public interface IChartSetProvider
    {
        bool TryGetChartSet(Guid chartSetGuid, out ChartSet chartSet);
        IEnumerable<ChartSet> GetAllChartSets();
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers
{
    using System;
    using Settings.Workspace;

    public interface IBuildInChartSetProvider
    {
        Guid ChartSetGuid { get; }

        BuildInChartSet GetChartSet();
    }
}
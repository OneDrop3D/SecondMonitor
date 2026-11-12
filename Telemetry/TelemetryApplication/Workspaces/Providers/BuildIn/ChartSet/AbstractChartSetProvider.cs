namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using SecondMonitor.ViewModels.Layouts;
    using Settings.Workspace;

    public abstract class AbstractChartSetProvider : IBuildInChartSetProvider
    {
        public abstract Guid ChartSetGuid { get; }
        public abstract string ChartSetName { get; }

        public BuildInChartSet GetChartSet()
        {
            return new BuildInChartSet()
            {
                Favourite = false,
                Guid = ChartSetGuid,
                Name = ChartSetName,
                LayoutDescription = GetLayoutDescription(),
            };
        }

        protected abstract LayoutDescription GetLayoutDescription();
    }
}
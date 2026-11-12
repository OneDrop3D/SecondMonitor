namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn
{
    using System;
    using System.Collections.Generic;
    using Settings.Workspace;

    public abstract class AbstractWorkspaceProvider : IBuildInWorkspaceProvider
    {
        public abstract Guid WorkspaceGuid { get; }

        public abstract string Name { get; }
        public BuildInWorkspace GetWorkspace()
        {
            return new BuildInWorkspace()
            {
                IsFavourite = false,
                ChartSetsGuid = GetChartSet(),
                Guid = WorkspaceGuid,
                Name = Name,
            };
        }

        protected abstract List<string> GetChartSet();
    }
}
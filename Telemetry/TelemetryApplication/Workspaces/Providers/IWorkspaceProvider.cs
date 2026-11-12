namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers
{
    using System;
    using System.Collections.Generic;
    using Settings.Workspace;

    public interface IWorkspaceProvider
    {
        bool TryGetWorkspace(Guid workspaceGuid, out Workspace workspace);

        IEnumerable<Workspace> GetAllWorkspaces();
    }
}
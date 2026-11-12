namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers
{
    using System;
    using Settings.Workspace;

    public interface IBuildInWorkspaceProvider
    {
        Guid WorkspaceGuid { get; }

        BuildInWorkspace GetWorkspace();
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Settings.Workspace;

    public class WorkspaceProvider : IWorkspaceProvider
    {
        private readonly List<IBuildInWorkspaceProvider> _buildInWorkspaces;
        private readonly ISettingsController _settingsController;

        public WorkspaceProvider(List<IBuildInWorkspaceProvider> buildInWorkspaces, ISettingsController settingsController)
        {
            _buildInWorkspaces = buildInWorkspaces;
            _settingsController = settingsController;
        }

        public bool TryGetWorkspace(Guid workspaceGuid, out Workspace workspace)
        {
            var storedWorkspace = _settingsController.TelemetrySettings.CustomWorkspaces.FirstOrDefault(x => x.Guid == workspaceGuid);
            if (storedWorkspace != null)
            {
                workspace = storedWorkspace;
                return true;
            }

            var buildInProvider = _buildInWorkspaces.FirstOrDefault(x => x.WorkspaceGuid == workspaceGuid);
            if (buildInProvider == null)
            {
                workspace = null;
                return false;
            }

            workspace = buildInProvider.GetWorkspace();
            return true;
        }

        public IEnumerable<Workspace> GetAllWorkspaces()
        {
            return _buildInWorkspaces.Select(x => x.GetWorkspace()).Concat(_settingsController.TelemetrySettings.CustomWorkspaces);
        }
    }
}
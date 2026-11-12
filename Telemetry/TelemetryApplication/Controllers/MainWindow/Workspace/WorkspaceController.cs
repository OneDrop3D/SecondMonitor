namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.Workspace
{
    using System;
    using System.Threading.Tasks;
    using GraphPanel;
    using Settings;
    using ViewModels;
    using ViewModels.Workspace;
    using Workspaces;

    public class WorkspaceController : IWorkspaceController
    {
        private readonly IWorkspacesSeriesChartsController _workspacesSeriesChartsController;
        private readonly IWorkspaceAggregatedChartsController _workspaceAggregatedChartsController;
        private readonly WorkspaceTabItemsFactory _workspaceTabItemsFactory;
        private readonly ISettingsController _settingsController;
        private readonly WorkspaceViewModel _workspaceViewModel;

        public WorkspaceController(IMainWindowViewModel mainWindowViewModel, IWorkspacesSeriesChartsController workspacesSeriesChartsController, IWorkspaceAggregatedChartsController workspaceAggregatedChartsController, WorkspaceTabItemsFactory workspaceTabItemsFactory, ISettingsController settingsController)
        {
            _workspacesSeriesChartsController = workspacesSeriesChartsController;
            _workspaceAggregatedChartsController = workspaceAggregatedChartsController;
            _workspaceTabItemsFactory = workspaceTabItemsFactory;
            _settingsController = settingsController;
            _workspaceViewModel = mainWindowViewModel.WorkspaceViewModel;
        }

        public async Task StartControllerAsync()
        {
            await _workspaceAggregatedChartsController.StartControllerAsync();
            await _workspacesSeriesChartsController.StartControllerAsync();
            _settingsController.SettingsChanged += SettingsControllerOnSettingsChanged;
            CreateWorkspaces();
        }

        public async Task StopControllerAsync()
        {
            await _workspaceAggregatedChartsController.StopControllerAsync();
            await _workspacesSeriesChartsController.StopControllerAsync();
            _settingsController.SettingsChanged -= SettingsControllerOnSettingsChanged;
        }

        private void CreateWorkspaces()
        {
            Guid selectedWorkspace = _settingsController.TelemetrySettings.SelectedWorkspaceGuid;
            _workspaceViewModel.ClearWorkspaceTabs();

            var workspaceTabs = _workspaceTabItemsFactory.CreateWorkspaceTabs(selectedWorkspace);
            if (workspaceTabs.Count == 0)
            {
                workspaceTabs = _workspaceTabItemsFactory.CreateDefaultWorkspaceTabs();
            }

            workspaceTabs.ForEach(_workspaceViewModel.AddWorkspaceTab);
        }

        private void SettingsControllerOnSettingsChanged(object sender, SettingChangedArgs e)
        {
            _workspacesSeriesChartsController.DisposeAllPreviouslyCreatedCharts();
            CreateWorkspaces();
        }
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.MainWindow.GraphPanel;
    using Providers;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Layouts.Factory;
    using SecondMonitor.ViewModels.Tabs;
    using SecondMonitor.ViewModels.Text;
    using Settings.Workspace;

    public class WorkspaceTabItemsFactory : IViewModelByNameFactory
    {
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly ILayoutFactory _layoutFactory;
        private readonly IChartSetProvider _chartSetProvider;
        private readonly IWorkspacesSeriesChartsController _workspacesSeriesChartsController;
        private readonly IWorkspaceAggregatedChartsController _workspaceAggregatedChartsController;

        public WorkspaceTabItemsFactory(IWorkspaceProvider workspaceProvider, ILayoutFactory layoutFactory, IChartSetProvider chartSetProvider, IWorkspacesSeriesChartsController workspacesSeriesChartsController, IWorkspaceAggregatedChartsController workspaceAggregatedChartsController)
        {
            _workspaceProvider = workspaceProvider;
            _layoutFactory = layoutFactory;
            _chartSetProvider = chartSetProvider;
            _workspacesSeriesChartsController = workspacesSeriesChartsController;
            _workspaceAggregatedChartsController = workspaceAggregatedChartsController;
        }

        public List<TabItemViewModel> CreateWorkspaceTabs(Guid workspaceGuid)
        {
            return !_workspaceProvider.TryGetWorkspace(workspaceGuid, out Workspace workspace) ? new List<TabItemViewModel>() : workspace.ChartSetsGuid.Select(x => CreateTabItem(Guid.Parse(x))).Where(x => x != null).ToList();
        }

        public List<TabItemViewModel> CreateDefaultWorkspaceTabs()
        {
            return new List<TabItemViewModel>();
        }

        private TabItemViewModel CreateTabItem(Guid chartSetGuid)
        {
            return !_chartSetProvider.TryGetChartSet(chartSetGuid, out ChartSet chartSet) ? null : new TabItemViewModel(new TitleViewModel(chartSet.Name), _layoutFactory.Create(chartSet.LayoutDescription.RootElement, this));
        }

        public IViewModel Create(string viewModelName)
        {
            return viewModelName.StartsWith(SeriesChartNames.SeriesNamePrefix) ? _workspacesSeriesChartsController.GetChartByName(viewModelName) : _workspaceAggregatedChartsController.GetChartByName(viewModelName);
        }
    }
}
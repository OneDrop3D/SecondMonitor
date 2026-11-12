namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.Workspace
{
    using System.Collections.ObjectModel;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Tabs;

    public class WorkspaceViewModel : AbstractViewModel
    {
        public WorkspaceViewModel()
        {
            WorkspaceTabs = new ObservableCollection<TabItemViewModel>();
        }

        public ObservableCollection<TabItemViewModel> WorkspaceTabs { get; }

        public void AddWorkspaceTab(TabItemViewModel newTab)
        {
            WorkspaceTabs.Add(newTab);
        }

        public void ClearWorkspaceTabs()
        {
            WorkspaceTabs.Clear();
        }
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.SettingsWindow.Workspace
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Layouts;
    using Settings.Workspace;

    public class WorkspacesSettingsViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private WorkspaceSettingViewModel _selectedWorkspaceSetting;
        private ChartSetSummaryViewModel _selectedChartSet;
        private ICommand _newWorkspaceCommand;
        private ICommand _duplicateWorkspaceCommand;
        private ICommand _importWorkspaceCommand;
        private ICommand _exportWorkspaceCommand;
        private ICommand _removeSelectedWorkspaceCommand;
        private ICommand _newChartListSetCommand;
        private ICommand _duplicateChartListCommand;
        private ICommand _removeSelectedChartListCommand;
        private ICommand _openChartListDetailCommand;

        public WorkspacesSettingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            Workspaces = new ObservableCollection<WorkspaceSettingViewModel>();
            AvailableChartSets = new ObservableCollection<ChartSetSummaryViewModel>();
            ChartSetMap = new Dictionary<Guid, ChartSet>();
        }

        public ObservableCollection<WorkspaceSettingViewModel> Workspaces { get; }

        public ObservableCollection<ChartSetSummaryViewModel> AvailableChartSets { get; }

        private Dictionary<Guid, ChartSet> ChartSetMap { get; }

        public WorkspaceSettingViewModel SelectedWorkspaceSetting
        {
            get => _selectedWorkspaceSetting;
            set => SetProperty(ref _selectedWorkspaceSetting, value);
        }

        public ChartSetSummaryViewModel SelectedChartSet
        {
            get => _selectedChartSet;
            set => SetProperty(ref _selectedChartSet, value);
        }

        public ICommand NewWorkspaceCommand
        {
            get => _newWorkspaceCommand;
            set => SetProperty(ref _newWorkspaceCommand, value);
        }

        public ICommand DuplicateWorkspaceCommand
        {
            get => _duplicateWorkspaceCommand;
            set => SetProperty(ref _duplicateWorkspaceCommand, value);
        }

        public ICommand ImportWorkspaceCommand
        {
            get => _importWorkspaceCommand;
            set => SetProperty(ref _importWorkspaceCommand, value);
        }

        public ICommand ExportWorkspaceCommand
        {
            get => _exportWorkspaceCommand;
            set => SetProperty(ref _exportWorkspaceCommand, value);
        }

        public ICommand RemoveSelectedWorkspaceCommand
        {
            get => _removeSelectedWorkspaceCommand;
            set => SetProperty(ref _removeSelectedWorkspaceCommand, value);
        }

        public ICommand NewChartListSetCommand
        {
            get => _newChartListSetCommand;
            set => SetProperty(ref _newChartListSetCommand, value);
        }

        public ICommand DuplicateChartListCommand
        {
            get => _duplicateChartListCommand;
            set => SetProperty(ref _duplicateChartListCommand, value);
        }

        public ICommand RemoveSelectedChartListCommand
        {
            get => _removeSelectedChartListCommand;
            set => SetProperty(ref _removeSelectedChartListCommand, value);
        }

        public ICommand OpenChartListDetailCommand
        {
            get => _openChartListDetailCommand;
            set => SetProperty(ref _openChartListDetailCommand, value);
        }

        public void ClearWorkspaces()
        {
            Workspaces.Clear();
        }

        public void AddWorkSpace(Workspace workspace)
        {
            var newViewModel = _viewModelFactory.Create<WorkspaceSettingViewModel>();
            newViewModel.ChartSetMap = ChartSetMap;
            newViewModel.FromModel(workspace);
            Workspaces.Add(newViewModel);
        }

        public void RemoveWorkspace(Guid workspaceId)
        {
            var toRemove = Workspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId);
            if (toRemove == null)
            {
                return;
            }

            Workspaces.Remove(toRemove);

            if (SelectedWorkspaceSetting == toRemove)
            {
                SelectedWorkspaceSetting = Workspaces.FirstOrDefault();
            }
        }

        public void RemoveSelectedWorkspace()
        {
            if (SelectedWorkspaceSetting == null)
            {
                return;
            }

            Workspaces.Remove(SelectedWorkspaceSetting);
            SelectedWorkspaceSetting = Workspaces.FirstOrDefault();
        }

        public void RefreshAvailableChartSets(IEnumerable<ChartSet> chartSets)
        {
            AvailableChartSets.Clear();
            ChartSetMap.Clear();
            foreach (ChartSet chartSet in chartSets)
            {
                var newViewModel = _viewModelFactory.Create<ChartSetSummaryViewModel>();
                newViewModel.FromModel(chartSet);
                AvailableChartSets.Add(newViewModel);
                ChartSetMap.Add(chartSet.Guid, chartSet);
            }
        }

        public void SelectedWorkspace(Guid workspaceId)
        {
            SelectedWorkspaceSetting = Workspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId);
        }

        public List<Workspace> GetNonBuildInWorkspaces()
        {
            return Workspaces.Where(x => !x.IsBuildIn).Select(x => x.SaveToNewModel()).ToList();
        }

        public List<ChartSet> GetNonBuildInChartSet()
        {
            return AvailableChartSets.Where(x => !x.IsBuildIn).Select(x => x.SaveToNewModel()).ToList();
        }

        public void AddChartSet(ChartSet newChartSet)
        {
            var newViewModel = _viewModelFactory.Create<ChartSetSummaryViewModel>();
            newViewModel.FromModel(newChartSet);
            AvailableChartSets.Add(newViewModel);
            ChartSetMap.Add(newChartSet.Guid, newChartSet);
        }

        public void SelectChartSet(Guid guid)
        {
            SelectedChartSet = AvailableChartSets.FirstOrDefault(x => x.Guid == guid);
        }

        public void RemoveSelectedChartList()
        {
            if (SelectedChartSet == null)
            {
                return;
            }

            Workspaces.ForEach(x => x.RemoveChartSet(SelectedChartSet.Guid));
            ChartSetMap.Remove(SelectedChartSet.Guid);
            AvailableChartSets.Remove(SelectedChartSet);
        }

        public void ReplaceChartSetLayout(Guid chartSetId, LayoutDescription layoutDescription)
        {
            var chartSet = AvailableChartSets.FirstOrDefault(x => x.Guid == chartSetId);
            if (chartSet == null)
            {
                return;
            }

            chartSet.LayoutDescription = layoutDescription;
        }
    }
}
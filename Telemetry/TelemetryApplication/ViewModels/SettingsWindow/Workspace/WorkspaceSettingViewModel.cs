namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.SettingsWindow.Workspace
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using Contracts.Commands;

    using GongSolutions.Wpf.DragDrop;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Adorners;
    using SecondMonitor.ViewModels.Factory;
    using Settings.Workspace;

    public class WorkspaceSettingViewModel : AbstractViewModel<Workspace>, IDropTarget
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isBuildIn;
        private string _name;
        private bool _isFavourite;
        private Guid _workspaceId;
        private ChartSetSummaryViewModel _selectedChartSet;

        public WorkspaceSettingViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            ChartSets = new ObservableCollection<ChartSetSummaryViewModel>();
        }

        public Dictionary<Guid, ChartSet> ChartSetMap { get; set; }

        public ChartSetSummaryViewModel SelectedChartSet
        {
            get => _selectedChartSet;
            set => SetProperty(ref _selectedChartSet, value);
        }

        public bool IsBuildIn
        {
            get => _isBuildIn;
            set => SetProperty(ref _isBuildIn, value);
        }

        public Guid WorkspaceId
        {
            get => _workspaceId;
            set => SetProperty(ref _workspaceId, value);
        }

        public ObservableCollection<ChartSetSummaryViewModel> ChartSets { get; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsFavourite
        {
            get => _isFavourite;
            set => SetProperty(ref _isFavourite, value);
        }

        protected override void ApplyModel(Workspace model)
        {
            IsBuildIn = model.IsBuildIn;
            WorkspaceId = model.Guid;
            Name = model.Name;
            IsFavourite = model.IsFavourite;
            ChartSets.Clear();
            foreach (Guid guid in model.ChartSetsGuid.Select(Guid.Parse))
            {
                AddChartListEntry(guid);
            }
        }

        private void AddChartListEntry(Guid guid, int index = -1)
        {
            if (!ChartSetMap.TryGetValue(guid, out ChartSet chartSet))
            {
                return;
            }

            AddChartListEntry(chartSet, index);
        }

        private void AddChartListEntry(ChartSet chartSet, int index)
        {
            var newViewModel = _viewModelFactory.Create<ChartSetSummaryViewModel>();
            newViewModel.IsDeleteButtonVisible = true;
            newViewModel.DeleteCommand = new RelayCommand(() => DeleteChartSet(newViewModel));
            newViewModel.FromModel(chartSet);

            if (index == -1)
            {
                ChartSets.Add(newViewModel);
            }
            else
            {
                ChartSets.Insert(index, newViewModel);
            }
        }

        private void DeleteChartSet(ChartSetSummaryViewModel viewModel)
        {
            ChartSets.Remove(viewModel);
        }

        public override Workspace SaveToNewModel()
        {
            return new Workspace()
            {
                Guid = WorkspaceId,
                Name = Name,
                IsFavourite = IsFavourite,
                ChartSetsGuid = ChartSets.Select(x => x.Guid.ToString()).ToList(),
            };
        }

        public void DragEnter(IDropInfo dropInfo)
        {
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var target = (dropInfo.VisualTarget as FrameworkElement)?.DataContext;
            switch (target)
            {
                case WorkspaceSettingViewModel _:
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.NotHandled = false;
                    return;
            }

            dropInfo.DropTargetAdorner = typeof(ForbidDropAdorner);
            dropInfo.Effects = DragDropEffects.None;
            dropInfo.NotHandled = false;
        }

        public void DragLeave(IDropInfo dropInfo)
        {
        }

        public void Drop(IDropInfo dropInfo)
        {
            var target = (dropInfo.VisualTarget as FrameworkElement)?.DataContext;
            if (!(dropInfo.Data is ChartSetSummaryViewModel chartSetSummaryViewModel) || !ReferenceEquals(target, this))
            {
                return;
            }

            AddChartListEntry(chartSetSummaryViewModel.OriginalModel, dropInfo.InsertIndex);
            ChartSets.Remove(chartSetSummaryViewModel);
        }

        public void UpdateChartSetName(Guid chartSetId, string newName)
        {
            ChartSets.Where(x => x.Guid == chartSetId).ForEach(x => x.Name = newName);
        }

        public void RemoveChartSet(Guid guid)
        {
            ChartSets.Where(x => x.Guid == guid).ToList().ForEach(x => ChartSets.Remove(x));
        }
    }
}
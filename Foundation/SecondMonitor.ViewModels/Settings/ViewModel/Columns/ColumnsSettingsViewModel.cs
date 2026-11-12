namespace SecondMonitor.ViewModels.Settings.ViewModel.Columns
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using Adorners;
    using Contracts.Commands;
    using DataGrid;
    using Factory;
    using GongSolutions.Wpf.DragDrop;
    using Model;

    public class ColumnsSettingsViewModel : AbstractViewModel<ColumnsSettings>, IDropTarget
    {
        private readonly ColumnDescriptorViewModelFactory _columnDescriptorViewModelFactory;
        private readonly IViewModelFactory _viewModelFactory;

        public ColumnsSettingsViewModel(ColumnDescriptorViewModelFactory columnDescriptorViewModelFactory, IViewModelFactory viewModelFactory)
        {
            _columnDescriptorViewModelFactory = columnDescriptorViewModelFactory;
            _viewModelFactory = viewModelFactory;
            ColumnDescriptorViewModels = new ObservableCollection<ColumnDescriptorViewModel>();
        }

        public ObservableCollection<ColumnDescriptorViewModel> ColumnDescriptorViewModels { get; }

        public List<ColumnDescriptor> ColumnDescriptors { get; set; }

        public static ColumnsSettingsViewModel CreateFromModel(ColumnsSettings columnsSettings, IViewModelFactory viewModelFactory)
        {
            ColumnsSettingsViewModel newColumnSettingsViewModel = viewModelFactory.Create<ColumnsSettingsViewModel>();
            newColumnSettingsViewModel.ApplyModel(columnsSettings);
            return newColumnSettingsViewModel;
        }

        protected override void ApplyModel(ColumnsSettings columnsSettings) => ApplyModel(columnsSettings.Columns);

        public void ApplyModel(IEnumerable<ColumnDescriptor> columnDescriptors)
        {
            ColumnDescriptors = columnDescriptors.ToList();
            RefreshColumnDescriptorsViewModels();
        }

        public void RefreshColumnDescriptorsViewModels()
        {
            ColumnDescriptorViewModels.Clear();
            foreach (var x in ColumnDescriptors.Select(_columnDescriptorViewModelFactory.CreateAndApplyModel))
            {
                x.RemoveColumnCommand = new RelayCommand(() => RemoveRow(x));
                ColumnDescriptorViewModels.Add(x);
            }
        }

        public override ColumnsSettings SaveToNewModel()
        {
            return new ColumnsSettings
            {
                Columns = ColumnDescriptors.ToArray()
            };
        }

        public void SaveColumnsDescriptors()
        {
            ColumnDescriptors = ColumnDescriptorViewModels.Select(x => x.SaveToNewModel()).ToList();
        }

        private void RemoveRow(ColumnDescriptorViewModel columnDescriptorViewModel)
        {
            ColumnDescriptorViewModels.Remove(columnDescriptorViewModel);
        }

        public void DragEnter(IDropInfo dropInfo)
        {
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var dragSource = (dropInfo.DragInfo.VisualSource as FrameworkElement)?.DataContext;
            if (dropInfo.Data is IColumnDescriptorTemplate)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else if (ReferenceEquals(dragSource, this) && dropInfo.Data is ColumnDescriptorViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.DropTargetAdorner = typeof(ForbidDropAdorner);
                dropInfo.Effects = DragDropEffects.None;
            }

            dropInfo.NotHandled = false;
        }

        public void DragLeave(IDropInfo dropInfo)
        {
        }

        public void Drop(IDropInfo dropInfo)
        {
            var target = (dropInfo.VisualTarget as FrameworkElement)?.DataContext;
            if (!ReferenceEquals(target, this))
            {
                return;
            }

            if (dropInfo.Data is IColumnDescriptorTemplate columnDescriptorTemplate)
            {
                var columnDescriptor = columnDescriptorTemplate.CreateColumnDescriptor();
                ColumnDescriptorViewModel newViewModel = _columnDescriptorViewModelFactory.CreateAndApplyModel(columnDescriptor);
                newViewModel.RemoveColumnCommand = new RelayCommand(() => RemoveRow(newViewModel));
                ColumnDescriptorViewModels.Insert(dropInfo.InsertIndex, newViewModel);
                dropInfo.NotHandled = false;
            }

            if (dropInfo.Data is ColumnDescriptorViewModel columnDescriptorViewModel)
            {
                int insertPosition = dropInfo.InsertIndex;
                int oldIndex = ColumnDescriptorViewModels.IndexOf(columnDescriptorViewModel);
                if (oldIndex < insertPosition)
                {
                    ColumnDescriptorViewModels.Move(oldIndex, Math.Max(0, insertPosition - 1));
                }
                else
                {
                    ColumnDescriptorViewModels.Move(oldIndex, Math.Min(ColumnDescriptorViewModels.Count - 1, insertPosition));
                }
            }
        }
    }
}
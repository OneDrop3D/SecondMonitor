namespace SecondMonitor.WindowsControls.WPF.DataGrid
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using SecondMonitor.DataModel.Extensions;

    using ViewModels.DataGrid;
    using ViewModels.Settings.Model.Layout;

    public class DynamicGrid : DataGrid
    {
        public static readonly DependencyProperty ColumnDescriptorsProperty = DependencyProperty.Register("ColumnDescriptors", typeof(IList<ColumnDescriptorViewModel>), typeof(DataGrid),
            new PropertyMetadata(default(IList<ColumnDescriptorViewModel>), HandleColumnDescriptorsPropertyChanged));

        private readonly Dictionary<ColumnDescriptorViewModel, DataGridColumn> _descriptorGridColumnMap;

        private readonly ColumnGenerator _columnGenerator;

        public DynamicGrid()
        {
            Loaded += DynamicGridLoaded;
            _descriptorGridColumnMap = new Dictionary<ColumnDescriptorViewModel, DataGridColumn>();
            _columnGenerator = new ColumnGenerator(18);
        }

        public IList<ColumnDescriptorViewModel> ColumnDescriptors
        {
            get => (IList<ColumnDescriptorViewModel>)GetValue(ColumnDescriptorsProperty);
            set => SetValue(ColumnDescriptorsProperty, value);
        }

        private bool DeferColumnsFromDescriptorGeneration { get; set; }

        private static void HandleColumnDescriptorsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DynamicGrid grid))
            {
                return;
            }

            if (e.OldValue is ObservableCollection<ColumnDescriptorViewModel> observableCollection)
            {
                observableCollection.CollectionChanged -= grid.OnCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<ColumnDescriptorViewModel> observableCollectionNew)
            {
                observableCollectionNew.CollectionChanged += grid.OnCollectionChanged;
            }

            if (grid.IsLoaded && e.NewValue != null)
            {
                grid.GenerateColumnsFromDescriptors();
            }
            else
            {
                grid.DeferColumnsFromDescriptorGeneration = true;
            }
        }

        private void DynamicGridLoaded(object sender, RoutedEventArgs e)
        {
            if (DeferColumnsFromDescriptorGeneration && ColumnDescriptors != null)
            {
                GenerateColumnsFromDescriptors();
                DeferColumnsFromDescriptorGeneration = false;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    e.NewItems.OfType<ColumnDescriptorViewModel>().ForEach(x => AddColumnDescriptorViewModel(x, e.NewStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    e.OldItems.OfType<ColumnDescriptorViewModel>().ForEach(RemoveColumnDescriptorViewModel);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    e.OldItems.OfType<ColumnDescriptorViewModel>().ForEach(RemoveColumnDescriptorViewModel);
                    e.NewItems.OfType<ColumnDescriptorViewModel>().ForEach(x => AddColumnDescriptorViewModel(x, e.NewStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveAllColumns();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RemoveColumnDescriptorViewModel(ColumnDescriptorViewModel columnDescriptor)
        {
            if (!_descriptorGridColumnMap.TryGetValue(columnDescriptor, out DataGridColumn dataGridColumn))
            {
                return;
            }

            columnDescriptor.PropertyChanged -= ColumnDescriptorOnPropertyChanged;
            RemoveColumn(columnDescriptor, dataGridColumn);
        }

        private void RemoveColumn(ColumnDescriptorViewModel columnDescriptor, DataGridColumn dataGridColumn)
        {
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn)).RemoveValueChanged(dataGridColumn, OnColumnWidthChanged);
            Columns.Remove(dataGridColumn);
            _descriptorGridColumnMap.Remove(columnDescriptor);
        }

        private void AddColumn(ColumnDescriptorViewModel columnDescriptorViewModel)
        {
            AddColumnDescriptorViewModel(columnDescriptorViewModel, Columns.Count);
        }

        private void AddColumnDescriptorViewModel(ColumnDescriptorViewModel columnDescriptorViewModel, int index)
        {
            if (index == -1)
            {
                return;
            }

            columnDescriptorViewModel.PropertyChanged += ColumnDescriptorOnPropertyChanged;
            AddColumn(columnDescriptorViewModel, index);
        }

        private void AddColumn(ColumnDescriptorViewModel columnDescriptorViewModel, int index)
        {
            if (_descriptorGridColumnMap.TryGetValue(columnDescriptorViewModel, out DataGridColumn oldColumn))
            {
                RemoveColumn(columnDescriptorViewModel, oldColumn);
            }

            var newColumn = _columnGenerator.Generate(columnDescriptorViewModel.ColumnDescriptor);
            Columns.Insert(index, newColumn);
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn)).AddValueChanged(newColumn, OnColumnWidthChanged);
            _descriptorGridColumnMap.Add(columnDescriptorViewModel, newColumn);
        }

        private void OnColumnWidthChanged(object sender, EventArgs e)
        {
            if (!(sender is DataGridColumn dataGridColumn))
            {
                return;
            }

            var columnDescriptorViewModel = _descriptorGridColumnMap.FirstOrDefault(x => x.Value == sender).Key;
            if (columnDescriptorViewModel != null && columnDescriptorViewModel.ColumnDescriptor.ColumnLength.SizeKind != SizeKind.Automatic)
            {
                columnDescriptorViewModel.ColumnDescriptor.ColumnLength.ManualSize = (int)dataGridColumn.ActualWidth;
            }
        }

        private void GenerateColumnsFromDescriptors()
        {
            RemoveAllColumns();
            ColumnDescriptors?.ForEach(AddColumn);
        }

        private void RemoveAllColumns()
        {
            var dependencyProperty = DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn));
            Columns.ForEach(x => dependencyProperty.RemoveValueChanged(x, OnColumnWidthChanged));
            Columns.Clear();
            _descriptorGridColumnMap.Clear();
        }

        private void ColumnDescriptorOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ColumnDescriptorViewModel.IsVisible) || !(sender is ColumnDescriptorViewModel columnDescriptorViewModel))
            {
                return;
            }

            switch (columnDescriptorViewModel.IsVisible)
            {
                case false when _descriptorGridColumnMap.TryGetValue(columnDescriptorViewModel, out DataGridColumn column):
                    RemoveColumn(columnDescriptorViewModel, column);
                    return;
                case true:
                {
                    int index = ColumnDescriptors.Where(x => x.IsVisible).IndexOf(x => x == columnDescriptorViewModel);
                    AddColumn(columnDescriptorViewModel, index);
                    break;
                }
            }
        }
    }
}
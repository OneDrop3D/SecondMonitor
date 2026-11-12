namespace SecondMonitor.WindowsControls.WPF.DynamicGrid
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Converters;
    using ViewModels;
    using ViewModels.Settings.Model.Layout;

    public partial class DynamicDefinedColumnsGrid : UserControl
    {
        public static readonly DependencyProperty ViewModelsProperty = DependencyProperty.Register("ViewModels", typeof(ICollection<IViewModel>), typeof(DynamicDefinedColumnsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        public static readonly DependencyProperty ColumnsWidthProperty = DependencyProperty.Register("ColumnsWidth", typeof(ICollection<LengthDefinitionSetting>), typeof(DynamicDefinedColumnsGrid),  new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        public static readonly DependencyProperty IsGridSplitterEnabledProperty = DependencyProperty.Register("IsGridSplitterEnabled", typeof(bool), typeof(DynamicDefinedColumnsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        public static readonly DependencyProperty GridSplitterBackgroundProperty = DependencyProperty.Register("GridSplitterBackground", typeof(Brush), typeof(DynamicDefinedColumnsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        private readonly SizeDefinitionToSizeConverter _sizeConverter;

        public DynamicDefinedColumnsGrid()
        {
            InitializeComponent();
            _sizeConverter = new SizeDefinitionToSizeConverter();
        }

        public ICollection<LengthDefinitionSetting> ColumnsWidth
        {
            get => (ICollection<LengthDefinitionSetting>)GetValue(ColumnsWidthProperty);
            set => SetValue(ColumnsWidthProperty, value);
        }

        public ICollection<IViewModel> ViewModels
        {
            get => (ICollection<IViewModel>)GetValue(ViewModelsProperty);
            set => SetValue(ViewModelsProperty, value);
        }

        public Brush GridSplitterBackground
        {
            get => (Brush)GetValue(GridSplitterBackgroundProperty);
            set => SetValue(GridSplitterBackgroundProperty, value);
        }

        public bool IsGridSplitterEnabled
        {
            get => (bool)GetValue(IsGridSplitterEnabledProperty);
            set => SetValue(IsGridSplitterEnabledProperty, value);
        }

        private static void OnViewModelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDefinedColumnsGrid dynamicColumnsGrid)
            {
                if (e.OldValue == e.NewValue)
                {
                    return;
                }

                dynamicColumnsGrid.TryUnsubscribe(e.OldValue as ICollection<IViewModel>);
                dynamicColumnsGrid.TrySubscribe(e.NewValue as ICollection<IViewModel>);
                dynamicColumnsGrid.RefreshMainGrid();
            }
        }

        private void RefreshMainGrid()
        {
            if (ViewModels == null || ColumnsWidth == null || ViewModels.Count != ColumnsWidth.Count)
            {
                MainGrid.Children.Clear();
                return;
            }

            List<IViewModel> viewModels = ViewModels.ToList();
            List<LengthDefinitionSetting> columnsWidth = ColumnsWidth.ToList();
            int numberOfColumns = columnsWidth.Count;

            if (numberOfColumns == 0)
            {
                MainGrid.Children.Clear();
                return;
            }

            MainGrid.Children.Clear();
            MainGrid.ColumnDefinitions.Clear();
            int columnIndex = 0;

            for (int i = 0; i < numberOfColumns; i++)
            {
                LengthDefinitionSetting columnWidth = columnsWidth[i];
                ColumnDefinition columnDefinition = new ColumnDefinition()
                {
                    Width = (GridLength)(_sizeConverter.Convert(columnWidth, typeof(GridLength), null, CultureInfo.InvariantCulture) ?? new GridLength()),
                };
                MainGrid.ColumnDefinitions.Add(columnDefinition);

                ContentPresenter contentPresenter = new ContentPresenter()
                {
                    Content = viewModels[i],
                };
                Grid.SetColumn(contentPresenter, columnIndex);
                MainGrid.Children.Add(contentPresenter);
                columnIndex++;

                if (!IsGridSplitterEnabled)
                {
                    continue;
                }

                columnDefinition = new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star),
                };

                MainGrid.ColumnDefinitions.Add(columnDefinition);

                GridSplitter separator = new GridSplitter()
                {
                    Width = 5,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = GridSplitterBackground,
                };
                Grid.SetColumn(separator, columnIndex);
                MainGrid.Children.Add(separator);
                columnIndex++;
            }
        }

        private void TryUnsubscribe(ICollection<IViewModel> viewModels)
        {
            if (viewModels is ObservableCollection<IViewModel> observableCollection)
            {
                observableCollection.CollectionChanged -= ObservableCollectionOnCollectionChanged;
            }
        }

        private void TrySubscribe(ICollection<IViewModel> viewModels)
        {
            if (viewModels is ObservableCollection<IViewModel> observableCollection)
            {
                observableCollection.CollectionChanged += ObservableCollectionOnCollectionChanged;
            }
        }

        private void ObservableCollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshMainGrid();
        }
    }
}
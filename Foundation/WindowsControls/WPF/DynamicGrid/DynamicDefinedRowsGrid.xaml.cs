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

    public partial class DynamicDefinedRowsGrid
    {
        public static readonly DependencyProperty ViewModelsProperty = DependencyProperty.Register("ViewModels", typeof(ICollection<IViewModel>), typeof(DynamicDefinedRowsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        public static readonly DependencyProperty RowsHeightProperty = DependencyProperty.Register("RowsHeight", typeof(ICollection<LengthDefinitionSetting>), typeof(DynamicDefinedRowsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        public static readonly DependencyProperty IsGridSplitterEnabledProperty = DependencyProperty.Register("IsGridSplitterEnabled", typeof(bool), typeof(DynamicDefinedRowsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));
        public static readonly DependencyProperty GridSplitterBackgroundProperty = DependencyProperty.Register("GridSplitterBackground", typeof(Brush), typeof(DynamicDefinedRowsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));

        private readonly SizeDefinitionToSizeConverter _sizeConverter;
        public DynamicDefinedRowsGrid()
        {
            InitializeComponent();
            _sizeConverter = new SizeDefinitionToSizeConverter();
        }

        public ICollection<LengthDefinitionSetting> RowsHeight
        {
            get => (ICollection<LengthDefinitionSetting>)GetValue(RowsHeightProperty);
            set => SetValue(RowsHeightProperty, value);
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
            if (d is DynamicDefinedRowsGrid dynamicRowsGrid)
            {
                if (e.OldValue == e.NewValue)
                {
                    return;
                }

                dynamicRowsGrid.TryUnsubscribe(e.OldValue as ICollection<IViewModel>);
                dynamicRowsGrid.TrySubscribe(e.NewValue as ICollection<IViewModel>);
                dynamicRowsGrid.RefreshMainGrid();
            }
        }

        private void RefreshMainGrid()
        {
            if (ViewModels == null || RowsHeight == null || ViewModels.Count != RowsHeight.Count)
            {
                MainGrid.Children.Clear();
                return;
            }

            List<IViewModel> viewModels = ViewModels.ToList();
            List<LengthDefinitionSetting> rowsHeight = RowsHeight.ToList();
            int numberOfRows = rowsHeight.Count;

            if (numberOfRows == 0)
            {
                MainGrid.Children.Clear();
                return;
            }

            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            int rowIndex = 0;

            for (int i = 0; i < numberOfRows; i++)
            {
                RowDefinition rowDefinition = new RowDefinition()
                {
                    Height = (GridLength)(_sizeConverter.Convert(rowsHeight[i], typeof(GridLength), null, CultureInfo.InvariantCulture) ?? new GridLength())
                };
                MainGrid.RowDefinitions.Add(rowDefinition);

                ContentPresenter contentPresenter = new ContentPresenter()
                {
                    Content = viewModels[i],
                };
                Grid.SetRow(contentPresenter, rowIndex);
                rowIndex++;
                MainGrid.Children.Add(contentPresenter);

                if (!IsGridSplitterEnabled)
                {
                    continue;
                }

                rowDefinition = new RowDefinition()
                {
                    Height = new GridLength(1, GridUnitType.Star),
                };
                MainGrid.RowDefinitions.Add(rowDefinition);

                GridSplitter separator = new GridSplitter()
                {
                    Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Background = GridSplitterBackground,
                };
                Grid.SetRow(separator, rowIndex);
                MainGrid.Children.Add(separator);
                rowIndex++;
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
namespace SecondMonitor.WindowsControls.WPF.DynamicGrid
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using ViewModels;

    /// <summary>
    /// Interaction logic for DynamicColumnsGrid.xaml
    /// </summary>
    public partial class DynamicColumnsGrid : UserControl
    {
        public static readonly DependencyProperty ViewModelsProperty =
           DependencyProperty.Register("ViewModels", typeof(ICollection<IViewModel>), typeof(DynamicColumnsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));

        public DynamicColumnsGrid()
        {
            InitializeComponent();
        }

        public ICollection<IViewModel> ViewModels
        {
            get => (ICollection<IViewModel>)GetValue(ViewModelsProperty);
            set => SetValue(ViewModelsProperty, value);
        }

        private static void OnViewModelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicColumnsGrid dynamicColumnsGrid)
            {
                dynamicColumnsGrid.TryUnsubscribe(e.OldValue as ICollection<IViewModel>);
                dynamicColumnsGrid.TrySubscribe(e.NewValue as ICollection<IViewModel>);
                dynamicColumnsGrid.RefreshMainGrid();
            }
        }

        private void RefreshMainGrid()
        {
            if (ViewModels == null)
            {
                MainGrid.Children.Clear();
                return;
            }

            List<IViewModel> viewModels = ViewModels.ToList();
            int numberOfColumns = viewModels.Count;

            if (numberOfColumns == 0)
            {
                MainGrid.Children.Clear();
                return;
            }

            MainGrid.Children.Clear();
            MainGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < numberOfColumns; i++)
            {
                ColumnDefinition rowDefinition = new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star),
                };
                MainGrid.ColumnDefinitions.Add(rowDefinition);

                ContentPresenter contentPresenter = new ContentPresenter()
                {
                    Content = viewModels[i],
                };
                Grid.SetColumn(contentPresenter, i);
                MainGrid.Children.Add(contentPresenter);
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

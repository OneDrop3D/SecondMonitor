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
    /// Interaction logic for DynamicRowsGrid.xaml
    /// </summary>
    public partial class DynamicRowsGrid : UserControl
    {
        public static readonly DependencyProperty ViewModelsProperty =
            DependencyProperty.Register("ViewModels", typeof(ICollection<IViewModel>), typeof(DynamicRowsGrid), new FrameworkPropertyMetadata(OnViewModelsPropertyChanged));

        public DynamicRowsGrid()
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
            if (d is DynamicRowsGrid dynamicRowsGrid)
            {
                dynamicRowsGrid.TryUnsubscribe(e.OldValue as ICollection<IViewModel>);
                dynamicRowsGrid.TrySubscribe(e.NewValue as ICollection<IViewModel>);
                dynamicRowsGrid.RefreshMainGrid();
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
            int numberOfRows = viewModels.Count;

            if (numberOfRows == 0)
            {
                MainGrid.Children.Clear();
                return;
            }

            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();

            for (int i = 0; i < numberOfRows; i++)
            {
                RowDefinition rowDefinition = new RowDefinition()
                {
                    Height = new GridLength(1, GridUnitType.Star),
                };
                MainGrid.RowDefinitions.Add(rowDefinition);

                ContentPresenter contentPresenter = new ContentPresenter()
                {
                    Content = viewModels[i],
                };
                Grid.SetRow(contentPresenter, i);
                MainGrid.Children.Add(contentPresenter);
                AddSeparator();
            }
        }

        private void AddSeparator()
        {
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

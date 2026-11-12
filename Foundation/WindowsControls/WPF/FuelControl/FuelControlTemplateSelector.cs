namespace SecondMonitor.WindowsControls.WPF.FuelControl
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    using SecondMonitor.ViewModels.CarStatus.FuelStatus;

    public class FuelControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ClassFuelDataTemplate { get; set; }

        public DataTemplate NewFuelDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not FuelOverviewViewModel fuelOverviewViewModel)
            {
                return base.SelectTemplate(item, container);
            }

            PropertyChangedEventHandler lambda = null;
            lambda = (o, args) =>
            {
                if (args.PropertyName == nameof(FuelOverviewViewModel.FuelVisualization))
                {
                    RebindContentTemplateSelector(container, fuelOverviewViewModel, lambda);
                }
            };

            fuelOverviewViewModel.PropertyChanged += lambda;

            switch (fuelOverviewViewModel.FuelVisualization)
            {
                case FuelVisualization.New:
                    return NewFuelDataTemplate;
                case FuelVisualization.Classic:
                default:
                    return ClassFuelDataTemplate;
            }
        }

        private void RebindContentTemplateSelector(DependencyObject container, FuelOverviewViewModel fuelOverviewViewModel, PropertyChangedEventHandler lambda)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RebindContentTemplateSelector(container, fuelOverviewViewModel, lambda));
                return;
            }

            fuelOverviewViewModel.PropertyChanged -= lambda;
            ContentPresenter cp = (ContentPresenter)container;
            cp.ContentTemplateSelector = null;
            cp.ContentTemplateSelector = this;
        }
    }
}

namespace SecondMonitor.WindowsControls.WPF.DriverPosition
{
    using System.Windows;
    using System.Windows.Controls;
    using ViewModels.Track;

    public class DriverTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DriverDataTemplate { get; set; }

        public DataTemplate SafetyCarDataTemplate { get; set; }

        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DriverPositionViewModel driverPositionViewModel)
            {
                return driverPositionViewModel.IsSafetyCar ? SafetyCarDataTemplate : DriverDataTemplate;
            }

            return DefaultTemplate;
        }
    }
}
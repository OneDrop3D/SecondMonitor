namespace SecondMonitor.WindowsControls.WPF.FuelControl
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.ViewModels.CarStatus.FuelStatus;

    public class HybridEnduranceStateToBrushConverter : IValueConverter
    {
        public Brush OkBrush { get; set; }
        public Brush WarningBrush { get; set; }
        public Brush ErrorBrush { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HybridEnduranceState d)
            {
                return OkBrush;
            }

            return d switch
            {
                HybridEnduranceState.Ok => OkBrush,
                HybridEnduranceState.Warning => WarningBrush,
                HybridEnduranceState.Error => ErrorBrush,
                _ => OkBrush,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

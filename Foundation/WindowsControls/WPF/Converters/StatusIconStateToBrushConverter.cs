namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using ViewModels.StatusIcon;

    public class StatusIconStateToBrushConverter : IValueConverter
    {
        public SolidColorBrush UnlitBrush { get; set; }
        public SolidColorBrush OkBrush { get; set; }
        public SolidColorBrush WarningBrush { get; set; }
        public SolidColorBrush ErrorBrush { get; set; }
        public SolidColorBrush InformationBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StatusIconState statusIconState)
            {
                return statusIconState switch
                {
                    StatusIconState.Unlit => UnlitBrush,
                    StatusIconState.Ok => OkBrush,
                    StatusIconState.Warning => WarningBrush,
                    StatusIconState.Error => ErrorBrush,
                    StatusIconState.Information => InformationBrush,
                    _ => UnlitBrush
                };
            }

            return UnlitBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
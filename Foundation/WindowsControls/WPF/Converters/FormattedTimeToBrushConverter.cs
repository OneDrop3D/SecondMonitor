namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class FormattedTimeToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush RedBrush;
        private static readonly SolidColorBrush GreenBrush;

        static FormattedTimeToBrushConverter()
        {
            RedBrush = (SolidColorBrush)Application.Current.FindResource("LightRed01Brush");
            GreenBrush = (SolidColorBrush)Application.Current.FindResource("Green01Brush");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string formattedTime)
            {
                return GreenBrush;
            }

            if (parameter is not string parameterString || !(bool.TryParse(parameterString, out bool invert)))
            {
                invert = false;
            }

            if (formattedTime.StartsWith("+"))
            {
                return invert ? GreenBrush : RedBrush;
            }
            else
            {
                return invert ? RedBrush : GreenBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
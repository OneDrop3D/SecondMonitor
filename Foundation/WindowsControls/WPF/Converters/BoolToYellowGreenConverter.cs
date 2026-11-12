namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class BoolToYellowGreenConverter : IValueConverter
    {
        private static readonly SolidColorBrush YellowBrush;
        private static readonly SolidColorBrush GreenBrush;

        static BoolToYellowGreenConverter()
        {
            YellowBrush = (SolidColorBrush)Application.Current.FindResource("Yellow01Brush");
            GreenBrush = (SolidColorBrush)Application.Current.FindResource("Green01Brush");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolValue))
            {
                return Brushes.Transparent;
            }

            return boolValue ? GreenBrush : YellowBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

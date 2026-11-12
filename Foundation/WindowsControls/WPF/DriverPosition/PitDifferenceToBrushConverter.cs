namespace SecondMonitor.WindowsControls.WPF.DriverPosition
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class PitDifferenceToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush PositiveBrush;
        private static readonly SolidColorBrush NegativeBrush;

        static PitDifferenceToBrushConverter()
        {
            PositiveBrush = (SolidColorBrush)Application.Current.FindResource("PitDifferencePositive");
            NegativeBrush = (SolidColorBrush)Application.Current.FindResource("PitDifferenceNegative");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string valueAsString))
            {
                return Brushes.Transparent;
            }

            return valueAsString.StartsWith("+") ? PositiveBrush : NegativeBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class DoubleSignToBrushConverter : IValueConverter
    {
        public Brush ZeroBrush { get; set; }
        public Brush NegativeBrush { get; set; }
        public Brush PositiveBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double dValue)
            {
                return ZeroBrush;
            }

            switch (dValue)
            {
                case 0:
                    return ZeroBrush;
                case < 0: return NegativeBrush;
                case > 0: return PositiveBrush;
                default:
                    return ZeroBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

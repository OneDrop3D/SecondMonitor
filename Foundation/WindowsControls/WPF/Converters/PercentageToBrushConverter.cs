namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class PercentageToBrushConverter : IValueConverter
    {
        public Brush OkBrush { get; set; }

        public Brush Below30Brush { get; set; }

        public Brush Below10Brush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double doubleValue)
            {
                return OkBrush;
            }

            return doubleValue switch
            {
                < 10 => Below10Brush,
                < 30 => Below30Brush,
                _ => OkBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class TimespanToHoursMinutesSecondsUnitSuffixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan timeSpan))
            {
                return string.Empty;
            }

            return timeSpan.Hours <= 0 ? $"{((int)(timeSpan.TotalSeconds / 60))}min {((int)timeSpan.TotalSeconds % 60):00}sec" : $"{timeSpan.Hours:0}h {timeSpan.Minutes:00}min {timeSpan.Seconds:00}sec";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
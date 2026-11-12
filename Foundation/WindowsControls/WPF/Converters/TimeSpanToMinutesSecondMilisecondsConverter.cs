namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using SecondMonitor.DataModel.Extensions;

    public class TimeSpanToMinutesSecondMilisecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan valueSpan)
            {
                return valueSpan == TimeSpan.Zero ? "-" : valueSpan.FormatToDefault();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class DateTimeToMonthDayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                char[] trimmer = DateTimeFormatInfo.CurrentInfo.DateSeparator.ToCharArray();
                string dateStr = date.ToString("d").Replace(date.ToString("yyyy"), string.Empty).Trim(trimmer);
                return dateStr;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
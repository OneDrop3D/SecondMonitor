namespace SecondMonitor.Timing.Application.Presentation.View
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap;

    public class BestLapToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "No Best Lap";
            }

            if (value is not ILapInfo lapInfo)
            {
                return string.Empty;
            }

            return lapInfo.Driver.DriverInfo.DriverShortName + "-(L" + lapInfo.LapNumber + "):"
                   + lapInfo.LapTime.FormatToDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

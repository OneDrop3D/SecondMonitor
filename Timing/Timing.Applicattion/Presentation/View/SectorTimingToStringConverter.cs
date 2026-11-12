namespace SecondMonitor.Timing.Application.Presentation.View
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap;

    public class SectorTimingToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return parameter;
            }

            if (value is not SectorTiming sectorTiming)
            {
                return string.Empty;
            }

            return "S" + sectorTiming.SectorNumber + ":" + sectorTiming.Lap.Driver.DriverInfo.DriverShortName + "-(L" + sectorTiming.Lap.LapNumber + "):"
                   + TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(sectorTiming.Duration, false);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

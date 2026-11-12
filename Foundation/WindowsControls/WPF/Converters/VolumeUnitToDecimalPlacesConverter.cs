namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using SecondMonitor.DataModel.BasicProperties;

    public class VolumeUnitToDecimalPlacesConverter : IValueConverter
    {
        public int LiterDecimalPlaces { get; set; } = 0;

        public int UsGallonDecimalPlaces { get; set; } = 1;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not VolumeUnits volumeUnits)
            {
                return 0;
            }

            switch (volumeUnits)
            {
                case VolumeUnits.Liters:
                    return LiterDecimalPlaces;
                case VolumeUnits.UsGallons:
                    return UsGallonDecimalPlaces;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

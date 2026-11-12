namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using ViewModels.Settings.Model.Layout;

    public class SizeDefinitionToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is LengthDefinitionSetting sizeDefinitionSetting))
            {
                return new GridLength(0, GridUnitType.Auto);
            }

            switch (sizeDefinitionSetting.SizeKind)
            {
                case SizeKind.Automatic:
                    return new GridLength(0, GridUnitType.Auto);
                case SizeKind.Remaining:
                    return new GridLength(sizeDefinitionSetting.ManualSize / 100.0, GridUnitType.Star);
                case SizeKind.Manual:
                    return new GridLength(sizeDefinitionSetting.ManualSize, GridUnitType.Pixel);
                default:
                    return new GridLength(0, GridUnitType.Auto);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
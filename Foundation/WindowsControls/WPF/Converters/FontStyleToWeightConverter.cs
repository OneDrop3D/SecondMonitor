namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    using FontStyle = SecondMonitor.ViewModels.Text.FontStyle;

    public class FontStyleToWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not FontStyle fontStyle)
            {
                return FontWeights.Normal;
            }

            switch (fontStyle)
            {
                case FontStyle.Bold:
                    return FontWeights.Bold;
                case FontStyle.Normal:
                default:
                    return FontWeights.Normal;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

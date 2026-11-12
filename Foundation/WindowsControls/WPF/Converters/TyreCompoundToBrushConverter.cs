namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class TyreCompoundToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush ultraSoftBrush;
        private static readonly SolidColorBrush superSoftBrush;
        private static readonly SolidColorBrush softBrush;
        private static readonly SolidColorBrush mediumBrush;
        private static readonly SolidColorBrush hardBrush;
        private static readonly SolidColorBrush intermediateBrush;
        private static readonly SolidColorBrush wetBrush;
        private static readonly SolidColorBrush unknownBrush;

        static TyreCompoundToBrushConverter()
        {
            ultraSoftBrush = (SolidColorBrush)Application.Current.FindResource("TyreUltraSoft");
            superSoftBrush = (SolidColorBrush)Application.Current.FindResource("TyreSuperSoft");
            softBrush = (SolidColorBrush)Application.Current.FindResource("TyreSoft");
            mediumBrush = (SolidColorBrush)Application.Current.FindResource("TyreMedium");
            hardBrush = (SolidColorBrush)Application.Current.FindResource("TyreHard");
            intermediateBrush = (SolidColorBrush)Application.Current.FindResource("TyreIntermediate");
            wetBrush = (SolidColorBrush)Application.Current.FindResource("TyreWet");
            unknownBrush = (SolidColorBrush)Application.Current.FindResource("Anthracite01Brush");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string stringValue))
            {
                return Brushes.Transparent;
            }

            stringValue = stringValue.ToLower();
            if (stringValue.Contains("ultrasoft"))
            {
                return ultraSoftBrush;
            }

            if (stringValue.Contains("supersoft"))
            {
                return superSoftBrush;
            }

            if (stringValue.Contains("soft") || stringValue.Contains("option"))
            {
                return softBrush;
            }

            if (stringValue.Contains("medium") || stringValue == "dry")
            {
                return mediumBrush;
            }

            if (stringValue.Contains("hard") || stringValue.Contains("prime"))
            {
                return hardBrush;
            }

            if (stringValue.Contains("inter"))
            {
                return intermediateBrush;
            }

            if (stringValue.Contains("wet") || stringValue.Contains("rain"))
            {
                return wetBrush;
            }

            if (stringValue.Contains("slick"))
            {
                return mediumBrush;
            }

            return unknownBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
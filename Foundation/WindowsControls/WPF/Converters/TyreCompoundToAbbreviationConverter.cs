namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class TyreCompoundToAbbreviationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string stringValue))
            {
                return string.Empty;
            }

            stringValue = stringValue.ToLower();
            if (stringValue.Contains("ultrasoft"))
            {
                return "US";
            }

            if (stringValue.Contains("supersoft"))
            {
                return "SS";
            }

            if (stringValue.Contains("soft"))
            {
                return "S";
            }

            if (stringValue.Contains("option"))
            {
                return "O";
            }

            if (stringValue.Contains("medium"))
            {
                return "M";
            }

            if (stringValue.Contains("hard"))
            {
                return "H";
            }

            if (stringValue.Contains("prime"))
            {
                return "P";
            }

            if (stringValue.Contains("inter"))
            {
                return "I";
            }

            if (stringValue.Contains("wet") || stringValue.Contains("rain"))
            {
                return "W";
            }

            if (stringValue.Contains("slick"))
            {
                return "S";
            }

            return stringValue.Length > 0 ? stringValue[0].ToString().ToUpper() : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
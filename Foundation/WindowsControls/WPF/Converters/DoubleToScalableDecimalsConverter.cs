namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class DoubleToScalableDecimalsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double valueD))
            {
                return 0;
            }

            if (double.IsInfinity(valueD) || double.IsNaN(valueD))
            {
                return "-";
            }

            if (parameter != null && int.TryParse(parameter.ToString(), out int decimalNumbers))
            {
                return valueD.ToString("F" + decimalNumbers);
            }

            double absValue = Math.Abs(valueD);
            return valueD == 0 ? "0" : absValue < 10 ? valueD.ToString("F2") : absValue < 100 ? valueD.ToString("F1") : valueD.ToString("F0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && double.TryParse(stringValue, out double returnValue))
            {
                return returnValue;
            }

            return 0;
        }
    }
}
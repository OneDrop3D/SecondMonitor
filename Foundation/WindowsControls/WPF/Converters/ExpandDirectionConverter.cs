namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using ViewModels.Settings.Model.Layout;

    public class ExpandDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ViewModels.Settings.Model.Layout.ExpandDirection modelExpandDirection))
            {
                return System.Windows.Controls.ExpandDirection.Up;
            }

            switch (modelExpandDirection)
            {
                case ExpandDirection.Up:
                    return System.Windows.Controls.ExpandDirection.Up;
                case ExpandDirection.Down:
                    return System.Windows.Controls.ExpandDirection.Down;
                case ExpandDirection.Left:
                    return System.Windows.Controls.ExpandDirection.Left;
                case ExpandDirection.Right:
                    return System.Windows.Controls.ExpandDirection.Right;
                default:
                    return System.Windows.Controls.ExpandDirection.Up;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
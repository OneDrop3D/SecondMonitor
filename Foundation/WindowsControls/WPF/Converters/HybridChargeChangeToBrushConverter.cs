namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.DataModel.Snapshot.Systems;

    public class HybridChargeChangeToBrushConverter : IValueConverter
    {
        public Brush NoneBrush { get; set; }

        public Brush RechargeBrush { get; set; }

        public Brush DischargeBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HybridChargeChange hybridChargeChange)
            {
                return NoneBrush;
            }

            switch (hybridChargeChange)
            {
                case HybridChargeChange.Discharge:
                    return DischargeBrush;
                case HybridChargeChange.Recharge:
                    return RechargeBrush;
                case HybridChargeChange.Unavailable:
                case HybridChargeChange.None:
                default:
                    return NoneBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

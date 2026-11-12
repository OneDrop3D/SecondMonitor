namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.DataModel.Snapshot.Systems;

    public class HybridSystemStatusToBrushConverter : IValueConverter
    {
        public Brush UnAvailableBrush { get; set; }

        public Brush OffBrush { get; set; }

        public Brush BalancedBrush { get; set; }

        public Brush AttackBrush { get; set; }

        public Brush BuildBrush { get; set; }

        public Brush QualificationBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HybridSystemMode hybridSystemStatus)
            {
                return UnAvailableBrush;
            }

            switch (hybridSystemStatus)
            {
                case HybridSystemMode.Off:
                    return OffBrush;
                case HybridSystemMode.Balanced:
                    return BalancedBrush;
                case HybridSystemMode.Attack:
                    return AttackBrush;
                case HybridSystemMode.Build:
                    return BuildBrush;
                case HybridSystemMode.Qualification:
                    return QualificationBrush;
                case HybridSystemMode.UnAvailable:
                default:
                    return UnAvailableBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

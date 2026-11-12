namespace SecondMonitor.WindowsControls.WPF.DriverPosition
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using ViewModels.Track;

    public class SectorStateToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush YellowBrush;
        private static readonly SolidColorBrush NormalBrush;
        private static readonly SolidColorBrush PurpleBrush;
        private static readonly SolidColorBrush PurpleClassBrush;
        private static readonly SolidColorBrush GreenBrush;

        static SectorStateToBrushConverter()
        {
            YellowBrush = (SolidColorBrush)Application.Current.FindResource("Yellow01Brush");
            PurpleClassBrush = (SolidColorBrush)Application.Current.FindResource("PurpleClassTimingBrush");
            NormalBrush = Brushes.Transparent;
            PurpleBrush = (SolidColorBrush)Application.Current.FindResource("PurpleTimingBrush");
            GreenBrush = (SolidColorBrush)Application.Current.FindResource("Green01Brush");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SectorState sectorState))
            {
                return Brushes.Transparent;
            }

            switch (sectorState)
            {
                case SectorState.Normal:
                    return NormalBrush;
                case SectorState.Yellow:
                    return YellowBrush;
                case SectorState.Purple:
                    return PurpleBrush;
                case SectorState.ClassPurple:
                    return PurpleClassBrush;
                case SectorState.PersonalBest:
                    return GreenBrush;
                default:
                    return NormalBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
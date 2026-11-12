namespace SecondMonitor.Rating.Presentation.Converters.Championship
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class PositionToBrushConverter : IValueConverter
    {
        private static readonly Brush firstPlaceBrush = (Brush)Application.Current.Resources["FirstPlaceBrush"];
        private static readonly Brush secondPlaceBrush = (Brush)Application.Current.Resources["SecondPlaceBrush"];
        private static readonly Brush thirdPlaceBrush = (Brush)Application.Current.Resources["ThirdPlaceBrush"];

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int position)
            {
                switch (position)
                {
                    case 1:
                        return firstPlaceBrush;
                    case 2:
                        return secondPlaceBrush;
                    case 3:
                        return thirdPlaceBrush;
                    default:
                        return Brushes.White;
                }
            }

            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
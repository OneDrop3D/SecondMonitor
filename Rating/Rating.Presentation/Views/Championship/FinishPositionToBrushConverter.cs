namespace SecondMonitor.Rating.Presentation.Views.Championship
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class FinishPositionToBrushConverter : IValueConverter
    {
        public Brush FirstPlaceBrush { get; set; }
        public Brush SecondPlaceBrush { get; set; }
        public Brush ThirdPlaceBrush { get; set; }
        public Brush DefaultBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int finishPosition)
            {
                switch (finishPosition)
                {
                    case 1:
                        return FirstPlaceBrush;
                    case 2:
                        return SecondPlaceBrush;
                    case 3:
                        return ThirdPlaceBrush;
                }
            }

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

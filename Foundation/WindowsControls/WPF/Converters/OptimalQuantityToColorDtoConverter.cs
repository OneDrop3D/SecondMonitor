namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Colors.Extensions;

    public class OptimalQuantityToColorDtoConverter : IValueConverter
    {
        public Color DefaultColor { get; set; }

        public Color IdealColor { get; set; }

        public Color LowColor { get; set; }

        public Color HighColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IOptimalQuantity optimalQuantity)
            {
                return new SolidColorBrush(DefaultColor);
            }

            Color color = ComputeColor(
                Math.Round(optimalQuantity.ActualQuantityRawValue),
                optimalQuantity.IdealQuantityRawValue,
                optimalQuantity.IdealQuantityWindowRawValue,
                optimalQuantity.IdealQuantityWindowRawValue * 2,
                optimalQuantity.IdealQuantityWindowRawValue * 2);

            return ColorDto.FromColor(color);
        }

        private Color ComputeColor(double value, double optimalValue, double window, double belowColoringThreshold, double aboveColoringThreshold)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return DefaultColor;
            }

            if (value < optimalValue - window - belowColoringThreshold)
            {
                return LowColor;
            }

            if (value > optimalValue + window + aboveColoringThreshold)
            {
                return HighColor;
            }

            if (value > optimalValue - window && value < optimalValue + window)
            {
                return IdealColor;
            }

            if (value < optimalValue)
            {
                double percentage = (value - (optimalValue - window - belowColoringThreshold))
                                    / (optimalValue - window - (optimalValue - window - belowColoringThreshold));
                return MediaColorExtension.InterpolateHslColor(LowColor, IdealColor, percentage);
            }
            else
            {
                double percentage = ((optimalValue + window) - value)
                                    / (optimalValue + window - (optimalValue + window + aboveColoringThreshold));

                return MediaColorExtension.InterpolateHslColor(IdealColor, HighColor, percentage);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

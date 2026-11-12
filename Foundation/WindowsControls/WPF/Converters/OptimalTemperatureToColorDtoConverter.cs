namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Colors.Extensions;

    public class OptimalTemperatureToColorDtoConverter : IValueConverter
    {
        public Color DefaultColor { get; set; }
        public Color LowQuantityColor { get; set; }
        public Color IdealQuantityColor { get; set; }
        public Color HighQuantityColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not OptimalQuantity<Temperature> optimalQuantity)
            {
                return DefaultColor;
            }

            Color color = ComputeColor(
                Math.Round(optimalQuantity.ActualQuantity.RawValue),
                optimalQuantity.IdealQuantity.RawValue,
                optimalQuantity.IdealQuantityWindow.RawValue,
                GetBelowColoringRange(optimalQuantity),
                GetAboveColoringRange(optimalQuantity));

            return color;
        }

        private Color ComputeColor(double value, double optimalValue, double window, double belowColoringThreshold, double aboveColoringThreshold)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return DefaultColor;
            }

            if (value < optimalValue - window - belowColoringThreshold)
            {
                return LowQuantityColor;
            }

            if (value > optimalValue + window + aboveColoringThreshold)
            {
                return HighQuantityColor;
            }

            if (value > optimalValue - window && value < optimalValue + window)
            {
                return IdealQuantityColor;
            }

            if (value < optimalValue)
            {
                double percentage = (value - (optimalValue - window - belowColoringThreshold))
                                    / (optimalValue - window - (optimalValue - window - belowColoringThreshold));
                return MediaColorExtension.InterpolateHslColor(LowQuantityColor, IdealQuantityColor, percentage);
            }
            else
            {
                double percentage = ((optimalValue + window) - value)
                                    / (optimalValue + window - (optimalValue + window + aboveColoringThreshold));
                return MediaColorExtension.InterpolateHslColor(IdealQuantityColor, HighQuantityColor, percentage);
            }
        }

        protected virtual double GetAboveColoringRange(OptimalQuantity<Temperature> optimalQuantity)
        {
            return optimalQuantity.IdealQuantityWindow.RawValue * 2;
        }

        protected virtual double GetBelowColoringRange(OptimalQuantity<Temperature> optimalQuantity)
        {
            return optimalQuantity.IdealQuantityWindow.RawValue * 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

namespace SecondMonitor.WindowsControls.WPF.WheelStatusControl
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using ViewModels.CarStatus;

    public class WheelSlipStatusToColorConverter : IValueConverter
    {
        public Brush NotSlippingBrush { get; set; }
        public Brush IsSlippingBrush { get; set; }
        public Brush WasSlippingBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IndicationStatus wheelSlipStatus)
            {
                return NotSlippingBrush;
            }

            switch (wheelSlipStatus)
            {
                case IndicationStatus.CurrentlyIndicating:
                    return IsSlippingBrush;
                case IndicationStatus.WasIndicating:
                    return WasSlippingBrush;
                case IndicationStatus.NoIndication:
                default:
                    return NotSlippingBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
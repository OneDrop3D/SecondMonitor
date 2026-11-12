namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using ViewModels.CarStatus.FuelStatus;

    public class RefuelingWindowStateToColorConverter : IValueConverter
    {
        public Brush ClosedBrush { get; set; }

        public Brush NoContingencyFuelBrush { get; set; }

        public Brush WithContingencyFuelBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RefuelingWindowState refuelingWindowState)
            {
                return ClosedBrush;
            }

            return refuelingWindowState switch
            {
                RefuelingWindowState.Closed => ClosedBrush,
                RefuelingWindowState.WithContingency => WithContingencyFuelBrush,
                RefuelingWindowState.NoContingency => NoContingencyFuelBrush,
                _ => ClosedBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
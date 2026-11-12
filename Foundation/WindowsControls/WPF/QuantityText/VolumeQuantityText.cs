namespace SecondMonitor.WindowsControls.WPF.QuantityText
{
    using System;
    using System.Windows;

    using DataModel.BasicProperties;

    public class VolumeQuantityText : AbstractQuantityText<Volume>
    {
        public static readonly DependencyProperty VolumeUnitsProperty = DependencyProperty.Register("VolumeUnits", typeof(VolumeUnits), typeof(VolumeQuantityText), new PropertyMetadata { DefaultValue = VolumeUnits.Liters, PropertyChangedCallback = QuantityChanged });
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(VolumeQuantityText), new PropertyMetadata(-1) { PropertyChangedCallback = QuantityChanged });

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public VolumeUnits VolumeUnits
        {
            get => (VolumeUnits)GetValue(VolumeUnitsProperty);
            set => SetValue(VolumeUnitsProperty, value);
        }

        protected override void UpdateIQuantity(double valueInUnits)
        {
        }

        protected override string GetUnitSymbol()
        {
            return Volume.GetUnitSymbol(VolumeUnits);
        }

        protected override double GetValueInUnits()
        {
            return DecimalPlaces >= 0 ? Math.Round(Quantity.GetValueInUnits(VolumeUnits), DecimalPlaces) : Quantity.GetValueInUnits(VolumeUnits);
        }
    }
}
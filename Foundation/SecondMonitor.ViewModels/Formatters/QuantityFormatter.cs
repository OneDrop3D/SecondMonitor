namespace SecondMonitor.ViewModels.Formatters
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.BasicProperties.FuelConsumption;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class QuantityFormatter : IQuantityFormatter
    {
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        public QuantityFormatter(ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public string Format(IQuantity quantity)
        {
            return quantity switch
            {
                Volume volume => Format(volume),
                Percentage percentage => FormatAsPercentage(percentage),
                FuelPerDistance fuelPerDistance => Format(fuelPerDistance),
                _ => string.Empty
            };
        }

        public string Format(Volume volume)
        {
            return volume.GetValueInUnits(_displaySettingsViewModel.VolumeUnits).ToStringScalableDecimals() + Volume.GetUnitSymbol(_displaySettingsViewModel.VolumeUnits);
        }

        public string Format(FuelPerDistance fuelPerDistance)
        {
            return fuelPerDistance.GetConsumption(_displaySettingsViewModel.FuelPerDistanceUnits).ToStringScalableDecimals() +
                   FuelPerDistance.GetUnitsSymbol(_displaySettingsViewModel.FuelPerDistanceUnits);
        }

        public string FormatAsRatio(Percentage percentage)
        {
            return percentage.InRatio.ToStringScalableDecimals();
        }

        public string FormatAsPercentage(Percentage percentage)
        {
            return percentage.InPercentage.ToStringScalableDecimals() + "%";
        }
    }
}

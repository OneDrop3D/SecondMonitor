namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Summary.FuelStrategy;

    public class StartingFuelMap : AbstractHumanReadableMap<FuelLoadKind>
    {
        public StartingFuelMap()
        {
            Translations.Add(FuelLoadKind.FullTank, "Full Tank");
            Translations.Add(FuelLoadKind.Auto, "Auto");
            Translations.Add(FuelLoadKind.Custom, "Custom Value");
        }
    }
}

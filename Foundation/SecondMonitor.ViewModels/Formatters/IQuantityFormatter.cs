namespace SecondMonitor.ViewModels.Formatters
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.BasicProperties.FuelConsumption;

    public interface IQuantityFormatter
    {
        string Format(IQuantity quantity);

        string Format(Volume volume);

        string Format(FuelPerDistance fuelPerDistance);

        string FormatAsRatio(Percentage percentage);

        string FormatAsPercentage(Percentage percentage);
    }
}

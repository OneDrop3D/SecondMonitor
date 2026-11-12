namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Summary.FuelStrategy;

    public class StintLengthMap : AbstractHumanReadableMap<StintLengthKind>
    {
        public StintLengthMap()
        {
            Translations.Add(StintLengthKind.Equal, "Equal Length");
            Translations.Add(StintLengthKind.LastShort, "Short Last Stint");
        }
    }
}

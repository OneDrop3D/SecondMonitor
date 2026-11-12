namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.Stint
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Summary.FuelStrategy.TimedStintStrategy;

    public class StintPitStopInfoMap : AbstractHumanReadableMap<StintPitStopInfo>
    {
        public StintPitStopInfoMap()
        {
            Translations.Add(StintPitStopInfo.FuelAtEndOfLap, "Fuel to Add, Current Lap");
            Translations.Add(StintPitStopInfo.FuelAtPitLap, "Fuel to Add, Pit Lap");
            Translations.Add(StintPitStopInfo.Both, "Both");
        }
    }
}

namespace SecondMonitor.DataModel.Summary.FuelStrategy.PitWindowStrategy
{
    using SecondMonitor.DataModel.BasicProperties;

    public class PitWindowStrategySettings
    {
        public bool IsRefuelingAllowed { get; set; } = true;

        public FuelLoadKind StartFuel { get; set; } = FuelLoadKind.Auto;

        public Volume StartFuelCustom { get; set; } = Volume.FromLiters(0);

        public int PitWindowPercentage { get; set; } = 50;

        public FuelLoadKind PitWindowFuel { get; set; } = FuelLoadKind.Auto;

        public Volume PitWindowFuelCustom { get; set; } = Volume.FromLiters(0);
    }
}

namespace SecondMonitor.DataModel.Summary.FuelStrategy.TankToTankStrategy
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;

    [Serializable]
    public class TankToTankStrategySettings
    {
        public FuelLoadKind StartFuel { get; set; } = FuelLoadKind.Auto;

        public Volume CustomStartingFuel { get; set; } = Volume.FromLiters(0);

        public StintLengthKind StintLength { get; set; } = StintLengthKind.LastShort;
    }
}

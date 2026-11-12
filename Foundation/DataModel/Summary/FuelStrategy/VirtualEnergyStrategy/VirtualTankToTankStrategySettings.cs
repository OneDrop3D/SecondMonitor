namespace SecondMonitor.DataModel.Summary.FuelStrategy.VirtualEnergyStrategy
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;

    [Serializable]
    public class VirtualTankToTankStrategySettings
    {
        public FuelLoadKind StartFuel { get; set; } = FuelLoadKind.Auto;

        public Volume CustomStartingFuel { get; set; } = Volume.FromLiters(0);

        public StintLengthKind StintLength { get; set; } = StintLengthKind.LastShort;

        public VirtualEnergyPitStopInfo PitStopInfo { get; set; } = VirtualEnergyPitStopInfo.TotalFuelEndOfLap;
    }
}
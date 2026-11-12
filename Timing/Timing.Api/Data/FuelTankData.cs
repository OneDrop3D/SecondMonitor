namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class FuelTankData
    {
        public FuelTankData(FuelInfo fuelInfo)
        {
            FuelCapacityLiters = fuelInfo.FuelCapacity.InLiters;
            FuelRemainingLiters = fuelInfo.FuelRemaining.InLiters;
            FuelPressureKpa = fuelInfo.FuelPressure.InKpa;
            VirtualTankPercentage = fuelInfo.VirtualTank.InPercentage;
            HasVirtualTank = fuelInfo.HasVirtualTank;
        }

        public double FuelCapacityLiters { get; }

        public double FuelRemainingLiters { get; }

        public double FuelPressureKpa { get; }

        public double VirtualTankPercentage { get; }

        public bool HasVirtualTank { get; }
    }
}

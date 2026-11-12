namespace SecondMonitor.DataModel.Summary
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class VirtualTankStatusSnapshot : QuantityStatusSnapshot<Percentage>
    {
        public VirtualTankStatusSnapshot(SimulatorDataSet dataSet) : base(dataSet)
        {
            FuelInfo fuelInfo = dataSet.PlayerInfo.CarInfo.FuelSystemInfo;
            QuantityLevel = fuelInfo.VirtualTank;
        }

        public Percentage VirtualTank => QuantityLevel;
    }
}

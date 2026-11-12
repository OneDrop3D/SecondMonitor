namespace SecondMonitor.DataModel.Summary
{
    using BasicProperties;
    using Snapshot;
    using Snapshot.Systems;

    public class FuelStatusSnapshot : QuantityStatusSnapshot<Volume>
    {
        public FuelStatusSnapshot(SimulatorDataSet dataSet) : base(dataSet)
        {
            FuelInfo fuelInfo = dataSet.PlayerInfo.CarInfo.FuelSystemInfo;
            QuantityLevel = fuelInfo.FuelRemaining;
        }

        public Volume FuelLevel => QuantityLevel;
    }
}
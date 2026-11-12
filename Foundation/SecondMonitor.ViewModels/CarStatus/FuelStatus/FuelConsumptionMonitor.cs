namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary;

    using SecondMonitor.DataModel.BasicProperties.FuelConsumption;

    public class FuelConsumptionMonitor : QuantityConsumptionMonitor<Volume, FuelPerDistance, FuelStatusSnapshot, FuelConsumptionInfo>
    {
        public FuelConsumptionMonitor()
        {
            ActPerMinute = Volume.FromLiters(0);
            ActPerLap = Volume.FromLiters(0);
            TotalPerMinute = Volume.FromLiters(0);
            TotalPerLap = Volume.FromLiters(0);
            TotalQuantityConsumptionInfo = new FuelConsumptionInfo();
        }

        public override string Name => "Fuel";

        public override void Reset()
        {
            base.Reset();
            TotalQuantityConsumptionInfo = new FuelConsumptionInfo();
            ActPerMinute = Volume.FromLiters(0);
            ActPerLap = Volume.FromLiters(0);
            TotalPerMinute = Volume.FromLiters(0);
            TotalPerLap = Volume.FromLiters(0);
        }

        public override Volume GetQuantityIn(double laps, SimulatorDataSet dataSet)
        {
            return Volume.FromLiters(dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters - TotalPerLap.InLiters * laps);
        }

        public override Volume GetMonitoredQuantity(SimulatorDataSet simulatorDataSet)
        {
            return simulatorDataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining;
        }

        protected override double GetQuantityInRawValue(SimulatorDataSet simulatorDataSet)
        {
            return simulatorDataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.RawValue;
        }

        protected override FuelConsumptionInfo CreateConsumptionInfo()
        {
            return new FuelConsumptionInfo();
        }

        protected override FuelConsumptionInfo CreateConsumptionInfo(FuelStatusSnapshot snapshot1, FuelStatusSnapshot snapshot2)
        {
            return FuelConsumptionInfo.CreateConsumption(snapshot1, snapshot2);
        }

        protected override FuelStatusSnapshot CreateSnapshot(SimulatorDataSet simulatorDataSet)
        {
            return new FuelStatusSnapshot(simulatorDataSet);
        }
    }
}
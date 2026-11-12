namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.BasicProperties.FuelConsumption;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary;

    public class VirtualEnergyConsumptionMonitor : QuantityConsumptionMonitor<Percentage, PercentagePerDistance, VirtualTankStatusSnapshot, VirtualTankConsumptionInfo>, IConsumptionMonitor
    {
        public VirtualEnergyConsumptionMonitor()
            : base()
        {
            TotalQuantityConsumptionInfo = new VirtualTankConsumptionInfo();
            ActPerMinute = Percentage.FromRatio(0);
            ActPerLap = Percentage.FromRatio(0);
            TotalPerMinute = Percentage.FromRatio(0);
            TotalPerLap = Percentage.FromRatio(0);
        }

        public override string Name => "Virtual";

        public override void Reset()
        {
            base.Reset();
            TotalQuantityConsumptionInfo = new VirtualTankConsumptionInfo();
            ActPerMinute = Percentage.FromRatio(0);
            ActPerLap = Percentage.FromRatio(0);
            TotalPerMinute = Percentage.FromRatio(0);
            TotalPerLap = Percentage.FromRatio(0);
        }

        protected override VirtualTankConsumptionInfo CreateConsumptionInfo()
        {
            return new VirtualTankConsumptionInfo();
        }

        protected override VirtualTankConsumptionInfo CreateConsumptionInfo(VirtualTankStatusSnapshot snapshot1, VirtualTankStatusSnapshot snapshot2)
        {
            return VirtualTankConsumptionInfo.CreateConsumption(snapshot1, snapshot2);
        }

        protected override VirtualTankStatusSnapshot CreateSnapshot(SimulatorDataSet simulatorDataSet)
        {
            return new VirtualTankStatusSnapshot(simulatorDataSet);
        }

        public override Percentage GetQuantityIn(double laps, SimulatorDataSet dataSet)
        {
            return Percentage.FromRatio(dataSet.PlayerInfo.CarInfo.FuelSystemInfo.VirtualTank.InRatio - TotalPerLap.InRatio * laps);
        }

        public override Percentage GetMonitoredQuantity(SimulatorDataSet simulatorDataSet)
        {
            return simulatorDataSet.PlayerInfo.CarInfo.FuelSystemInfo.VirtualTank;
        }

        protected override double GetQuantityInRawValue(SimulatorDataSet simulatorDataSet)
        {
            return GetMonitoredQuantity(simulatorDataSet).RawValue;
        }
    }
}

namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;

    public interface IConsumptionMonitor
    {
        public string Name { get; }
        public IQuantity ActPerMinute { get; }

        public IQuantity ActPerLap { get; }

        public IQuantity TotalPerMinute { get; }

        public IQuantity TotalPerLap { get; }

        void Reset();

        IQuantity GetMonitoredQuantity(SimulatorDataSet simulatorDataSet);

        IQuantity GetQuantityIn(double laps, SimulatorDataSet dataSet);

        void UpdateQuantityConsumption(SimulatorDataSet simulatorDataSet);

        double? GetLapsLeft(SimulatorDataSet simulatorDataSet);

        TimeSpan? GetTimeLeft(SimulatorDataSet simulatorDataSet);

        int GetLapsOfFuelLeftAtNextLapStart();
    }
}

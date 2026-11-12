namespace SecondMonitor.ViewModels.FuelConsumption
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary.FuelConsumption;

    public interface ISessionFuelCalculatorConfiguration
    {
        public SessionType SessionType { get; }

        public (int Laps, int Minutes, int ExtraFuelLiters) GetRequiredRunTime(SimulatorDataSet dataSet, OverallFuelConsumptionHistory fuelConsumptionHistory);

        public bool ShouldAutoOpen(SimulatorDataSet dataSet);

        public bool ShouldAutoClose(SimulatorDataSet dataSet);

        public void OnUserClosed();

        public void OnAutoClose();

        public void Reset();
    }
}
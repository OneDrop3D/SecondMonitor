namespace SecondMonitor.ViewModels.FuelConsumption
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary.FuelConsumption;

    public class SessionFuelConfigurationNull : ISessionFuelCalculatorConfiguration
    {
        public SessionType SessionType => SessionType.Na;

        public (int Laps, int Minutes, int ExtraFuelLiters) GetRequiredRunTime(SimulatorDataSet dataSet, OverallFuelConsumptionHistory fuelConsumptionHistory)
        {
            return (0, 0, 0);
        }

        public bool ShouldAutoOpen(SimulatorDataSet dataSet)
        {
            return false;
        }

        public bool ShouldAutoClose(SimulatorDataSet dataSet)
        {
            return false;
        }

        public void OnUserClosed()
        {
        }

        public void OnAutoClose()
        {
        }

        public void Reset()
        {
        }
    }
}
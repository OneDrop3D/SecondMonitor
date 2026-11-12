namespace SecondMonitor.ViewModels.FuelConsumption
{
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Controllers;

    public interface IFuelStrategyController : IChildController<FuelConsumptionController>
    {
        void ApplyDataSet(SimulatorDataSet dataSet);
    }
}

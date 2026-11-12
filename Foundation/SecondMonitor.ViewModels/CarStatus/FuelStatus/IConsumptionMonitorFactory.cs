namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel.Snapshot;

    public interface IConsumptionMonitorFactory
    {
        IReadOnlyList<IConsumptionMonitor> Create(SimulatorDataSet dataSet);
    }
}

namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel.Snapshot;

    public class ConsumptionMonitorFactory : IConsumptionMonitorFactory
    {
        private readonly VirtualEnergyCondition _virtualEnergyCondition;
        public ConsumptionMonitorFactory(VirtualEnergyCondition virtualEnergyCondition)
        {
            _virtualEnergyCondition = virtualEnergyCondition;
        }

        public IReadOnlyList<IConsumptionMonitor> Create(SimulatorDataSet dataSet)
        {
            List<IConsumptionMonitor> consumptionMonitors = new List<IConsumptionMonitor>() { new FuelConsumptionMonitor() };
            if (_virtualEnergyCondition.HasVirtualEnergy(dataSet))
            {
                consumptionMonitors.Add(new VirtualEnergyConsumptionMonitor());
            }

            return consumptionMonitors;
        }
    }
}

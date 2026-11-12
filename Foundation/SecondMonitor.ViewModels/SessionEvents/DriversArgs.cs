namespace SecondMonitor.ViewModels.SessionEvents
{
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    public class DriversArgs : DataSetArgs
    {
        public DriversArgs(SimulatorDataSet currentDataSet, SimulatorDataSet previousDataSet, IEnumerable<DriverInfo> driverInfo) : base(currentDataSet)
        {
            PreviousDataSet = previousDataSet;
            DriverInfo = driverInfo.ToList();
        }

        public SimulatorDataSet PreviousDataSet { get; }

        public IReadOnlyCollection<DriverInfo> DriverInfo { get; }
    }
}
namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.SessionEvents;

    public class VirtualEnergyCondition
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        public VirtualEnergyCondition(ISessionEventProvider sessionEventProvider)
        {
            _sessionEventProvider = sessionEventProvider;
        }

        public bool HasVirtualEnergy(SimulatorDataSet dataSet)
        {
            return (dataSet.Source == SimulatorsNameMap.LmUSimName && dataSet.PlayerInfo?.CarClassId is SimulatorsNameMap.LmUHyperCarClassId or SimulatorsNameMap.LmUGT3CarClassId)
                   || (dataSet.Source == SimulatorsNameMap.R3ESimName & _sessionEventProvider.CurrentCarInfo?.FuelSystemInfo.HasVirtualTank == true);
        }
    }
}

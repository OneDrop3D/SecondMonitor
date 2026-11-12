namespace SecondMonitor.ViewModels.FuelConsumption
{
    using System.Threading.Tasks;

    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.CarStatus;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.SessionEvents;

    public class HybridOverviewController : IChildController<FuelConsumptionController>
    {
        private readonly HybridOverviewViewModel _hybridOverviewViewModel;
        private readonly ILapCompletedEventProvider _lapCompletedEventProvider;
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly HybridConsumptionMonitor _hybridConsumptionMonitor;

        public HybridOverviewController(HybridOverviewViewModel hybridOverviewViewModel, ILapCompletedEventProvider lapCompletedEventProvider, SessionRemainingCalculator sessionRemainingCalculator, ISessionEventProvider sessionEventProvider)
        {
            _hybridOverviewViewModel = hybridOverviewViewModel;
            _lapCompletedEventProvider = lapCompletedEventProvider;
            _sessionRemainingCalculator = sessionRemainingCalculator;
            _sessionEventProvider = sessionEventProvider;
            _hybridConsumptionMonitor = new HybridConsumptionMonitor();
        }

        public FuelConsumptionController ParentController { get; set; }

        public Task StartControllerAsync()
        {
            _lapCompletedEventProvider.LapCompletedSummary += LapCompletedEventProviderOnLapCompletedSummary;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _lapCompletedEventProvider.LapCompletedSummary -= LapCompletedEventProviderOnLapCompletedSummary;
            return Task.CompletedTask;
        }

        public void ApplyDataSet(SimulatorDataSet dataSet)
        {
            if (dataSet.PlayerInfo == null)
            {
                return;
            }

            _hybridConsumptionMonitor.UpdateConsumption(dataSet);
            _hybridOverviewViewModel.FromModel(_hybridConsumptionMonitor);
        }

        public void Reset()
        {
            _hybridConsumptionMonitor.Reset();
        }

        public void LapCompletedEventProviderOnLapCompletedSummary(object sender, LapSummaryArgs e)
        {
            SimulatorDataSet lastDataSet = _sessionEventProvider.LastDataSet;
            if (!e.DriverInfo.IsPlayer || lastDataSet == null)
            {
                return;
            }

            if (lastDataSet.SessionInfo.SessionType == SessionType.Race)
            {
                double lapsToGo = _sessionRemainingCalculator.GetLapsRemaining(lastDataSet);
                _hybridConsumptionMonitor.OnLapCompletedInRace(e.DriverInfo, lapsToGo);
            }
            else
            {
                _hybridConsumptionMonitor.OnLapCompletedNonRace(e.DriverInfo);
            }
        }
    }
}

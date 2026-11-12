namespace SecondMonitor.Timing.Application.Controllers
{
    using DataModel.Snapshot;
    using ViewModels.Controllers;

    public interface ISessionEventsController : IController, ISimulatorDataSetVisitor
    {
        void UpdateLastDataSet(SimulatorDataSet simulatorDataSet);
    }
}
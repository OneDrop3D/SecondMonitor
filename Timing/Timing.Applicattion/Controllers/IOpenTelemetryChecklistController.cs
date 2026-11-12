namespace SecondMonitor.Timing.Application.Controllers
{
    using Common.SessionTiming.Drivers;
    using ViewModels.Controllers;

    public interface IOpenTelemetryChecklistController : IController
    {
        void OpenLapSelection(DriverTiming sessionTimingPlayer);
    }
}
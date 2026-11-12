namespace SecondMonitor.Telemetry.TelemetryApplication.Checks
{
    using SecondMonitor.ViewModels.Controllers;
    using TelemetryManagement.DTO;

    public interface ITelemetryCheckController : IController
    {
        void OpenCheckWindow(LapTelemetryDto lapTelemetryDto);
    }
}
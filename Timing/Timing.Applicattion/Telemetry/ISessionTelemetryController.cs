namespace SecondMonitor.Timing.Application.Telemetry
{
    using Common.SessionTiming.Drivers.Lap;
    using ViewModels.Controllers;

    public interface ISessionTelemetryController : IController
    {
        void TrySaveLapTelemetry(ILapInfo lapInfo);
    }
}
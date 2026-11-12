namespace SecondMonitor.Timing.Application.Telemetry
{
    using DataModel.Snapshot;

    public interface ISessionTelemetryControllerFactory
    {
        ISessionTelemetryController Create(SimulatorDataSet simulatorDataSet);
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.DataAdapters
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public interface IQuantityAdapter<out T> where T : class, IQuantity
    {
        bool IsQuantityComputed(SimulatorSourceInfo sourceInfo);

        T GetQuantity(TelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto);
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.DataAdapters
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public interface IWheelQuantityAdapter<out T> where T : class, IQuantity
    {
        bool IsQuantityComputed(SimulatorSourceInfo simulatorSource);

        T GetQuantityFromWheel(SimulatorSourceInfo simulatorSource, WheelInfo wheelInfo, CarPropertiesDto carProperties);
    }
}
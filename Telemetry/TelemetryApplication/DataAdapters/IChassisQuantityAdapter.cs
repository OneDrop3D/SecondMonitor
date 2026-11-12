namespace SecondMonitor.Telemetry.TelemetryApplication.DataAdapters
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public interface IChassisQuantityAdapter<out T> where T : class, IQuantity
    {
        bool IsQuantityComputed(SimulatorSourceInfo simulatorSource);

        T GetQuantityFront(SimulatorSourceInfo simulatorSource, CarInfo carInfo, CarPropertiesDto carPropertiesDto);

        T GetQuantityBack(SimulatorSourceInfo simulatorSource, CarInfo carInfo, CarPropertiesDto carPropertiesDto);
    }
}
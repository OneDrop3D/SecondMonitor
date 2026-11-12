namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.DefaultCarProperties
{
    using TelemetryManagement.Settings;

    public interface IDefaultCarPropertiesProvider
    {
        bool TryGetDefaultSettings(string simName, string carName, out CarPropertiesDto carPropertiesDto);
    }
}
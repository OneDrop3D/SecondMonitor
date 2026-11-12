namespace SecondMonitor.Telemetry.TelemetryManagement.Migrations
{
    using DTO;

    public interface ITelemetryMigration
    {
        int TargetVersion { get; }
        void MigrateUp(LapTelemetryDto lapTelemetryDto);
    }
}
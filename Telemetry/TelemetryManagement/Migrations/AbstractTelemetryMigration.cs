namespace SecondMonitor.Telemetry.TelemetryManagement.Migrations
{
    using DTO;

    public abstract class AbstractTelemetryMigration : ITelemetryMigration
    {
        public abstract int TargetVersion { get; }
        public void MigrateUp(LapTelemetryDto lapTelemetryDto)
        {
            MigrateUpInternal(lapTelemetryDto);
            lapTelemetryDto.Version = TargetVersion + 1;
        }

        protected abstract void MigrateUpInternal(LapTelemetryDto lapTelemetryDto);
    }
}
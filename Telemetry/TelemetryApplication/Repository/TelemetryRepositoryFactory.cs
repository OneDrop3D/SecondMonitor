namespace SecondMonitor.Telemetry.TelemetryApplication.Repository
{
    using System.Collections.Generic;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Migrations;
    using TelemetryManagement.Repository;

    public class TelemetryRepositoryFactory : ITelemetryRepositoryFactory
    {
        private readonly List<ITelemetryMigration> _telemetryMigrations;

        public TelemetryRepositoryFactory(List<ITelemetryMigration> telemetryMigrations)
        {
            _telemetryMigrations = telemetryMigrations;
        }

        public ITelemetryRepository Create(ISettingsProvider settingsProvider)
        {
            return new TelemetryRepository(settingsProvider.TelemetryRepositoryPath, settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.MaxSessionsKept, _telemetryMigrations);
        }
    }
}
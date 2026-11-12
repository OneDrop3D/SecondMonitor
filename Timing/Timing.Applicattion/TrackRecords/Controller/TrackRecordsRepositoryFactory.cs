namespace SecondMonitor.Timing.Application.TrackRecords.Controller
{
    using System.Collections.Generic;

    using SecondMonitor.Timing.Application.TrackRecords.Controller.Migrations;

    using ViewModels.Settings;

    public class TrackRecordsRepositoryFactory : ITrackRecordsRepositoryFactory
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IEnumerable<ISimulatorsRecordsMigration> _migrations;

        public TrackRecordsRepositoryFactory(ISettingsProvider settingsProvider, IEnumerable<ISimulatorsRecordsMigration> migrations)
        {
            _settingsProvider = settingsProvider;
            _migrations = migrations;
        }

        public TrackRecordsRepository Create()
        {
            return new TrackRecordsRepository(_settingsProvider.TrackRecordsPath, _migrations);
        }
    }
}
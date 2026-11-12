namespace SecondMonitor.Timing.Application.TrackRecords.Controller
{
    using System.Collections.Generic;

    using DataModel.TrackRecords;

    using SecondMonitor.Timing.Application.TrackRecords.Controller.Migrations;

    using ViewModels.Repository;

    public class TrackRecordsRepository : AbstractXmlRepositoryWithMigration<SimulatorsRecords>
    {
        public TrackRecordsRepository(string directory, IEnumerable<ISimulatorsRecordsMigration> migrations) : base(migrations)
        {
            RepositoryDirectory = directory;
        }

        protected override string RepositoryDirectory { get; }
        protected override string FileName => "TrackRecords.xml";
    }
}
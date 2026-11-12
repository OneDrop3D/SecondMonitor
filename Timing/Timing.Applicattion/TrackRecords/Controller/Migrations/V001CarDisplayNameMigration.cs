namespace SecondMonitor.Timing.Application.TrackRecords.Controller.Migrations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.TrackRecords;

    public class V001CarDisplayNameMigration : ISimulatorsRecordsMigration
    {
        public int VersionAfterMigration => 1;

        public SimulatorsRecords ApplyMigration(SimulatorsRecords oldVersion)
        {
            foreach (TrackRecord trackRecord in oldVersion.SimulatorRecords.SelectMany(x => x.TrackRecords))
            {
                List<RecordSet> recordSets = trackRecord.ClassRecords
                    .Concat(trackRecord.VehicleRecords)
                    .Concat(new List<RecordSet>() { trackRecord.OverallRecord })
                    .Where(x => x != null)
                    .ToList();

                foreach (IEnumerable<RecordEntryDto> recordEntries in recordSets.Select(recordSet =>
                             new List<RecordEntryDto>() { recordSet.BestPracticeRecord, recordSet.BestQualiRecord, recordSet.BestRaceRecord }.Where(x => x != null)))
                {
                    recordEntries.ForEach(x => x.CarDisplayName = x.CarName);
                }
            }

            return oldVersion;
        }
    }
}

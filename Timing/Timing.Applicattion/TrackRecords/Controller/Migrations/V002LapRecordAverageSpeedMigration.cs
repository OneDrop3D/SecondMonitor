namespace SecondMonitor.Timing.Application.TrackRecords.Controller.Migrations
{
    using System;

    using SecondMonitor.DataModel.SimulatorContent;
    using SecondMonitor.DataModel.TrackRecords;
    using SecondMonitor.ViewModels.SimulatorContent;

    public class V002LapRecordAverageSpeedMigration : ISimulatorsRecordsMigration
    {
        private readonly ISimulatorContentProvider _simulatorContentProvider;

        public V002LapRecordAverageSpeedMigration(ISimulatorContentProvider simulatorContentProvider)
        {
            _simulatorContentProvider = simulatorContentProvider;
        }

        public int VersionAfterMigration => 2;

        public SimulatorsRecords ApplyMigration(SimulatorsRecords oldVersion)
        {
            oldVersion.SimulatorRecords.ForEach(Update);
            return oldVersion;
        }

        private void Update(SimulatorRecords simulatorRecords)
        {
            simulatorRecords.TrackRecords.ForEach(x => Update(x, simulatorRecords.SimulatorName));
        }

        private void Update(TrackRecord trackRecord, string simulatorName)
        {
            if (!_simulatorContentProvider.TryGetTrack(simulatorName, trackRecord.TrackName, out Track track))
            {
                return;
            }

            double layoutLengthInKm = track.LapDistance / 1000.0;
            ComputeAverageSpeed(trackRecord.OverallRecord, layoutLengthInKm);
            trackRecord.ClassRecords.ForEach(x => ComputeAverageSpeed(x, layoutLengthInKm));
            trackRecord.VehicleRecords.ForEach(x => ComputeAverageSpeed(x, layoutLengthInKm));
        }

        private void ComputeAverageSpeed(RecordSet trackRecordOverallRecord, double layoutLengthInKm)
        {
            ComputeAverageSpeed(trackRecordOverallRecord.BestPracticeRecord, layoutLengthInKm);
            ComputeAverageSpeed(trackRecordOverallRecord.BestQualiRecord, layoutLengthInKm);
            ComputeAverageSpeed(trackRecordOverallRecord.BestRaceRecord, layoutLengthInKm);
        }

        private void ComputeAverageSpeed(RecordEntryDto recordEntry, double layoutLengthInKm)
        {
            if (recordEntry == null || recordEntry.LapTime <= TimeSpan.Zero)
            {
                return;
            }

            recordEntry.AverageSpeedInKph = layoutLengthInKm / recordEntry.LapTime.TotalHours;
        }
    }
}

namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker
{
    using DataModel.Snapshot;

    public interface ISpecificLapTracker
    {
        string Name { get; }

        void OnSectionEntryCreated(SimulatorDataSet simulatorDataSet, DriverTiming driverTiming, in SectionRecord newRecord, in SectionRecord replacedRecord, bool isFirstInBatch, bool isLastInBatch, DriverLapSectorsTracker parentTracker);
    }
}
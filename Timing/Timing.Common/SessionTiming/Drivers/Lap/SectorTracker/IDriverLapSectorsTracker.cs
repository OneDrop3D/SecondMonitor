namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker
{
    using System;

    public interface IDriverLapSectorsTracker
    {
        void Update();

        TimeSpan GetSectionTime(double lapDistance);

        TimeSpan GetRelativeGapToPlayer();

        TimeSpan GetGapToPlayerAbsolute();
        TimeSpan GetRelativeGapToLeader();

        TimeSpan GetGapToDriverAbsolute(DriverTiming otherDriver);

        TimeSpan GetGapToDriverRelative(DriverTiming otherDriver);

        ref readonly SectionRecord GetSectionRecord(double lapDistance);

        SectionRecord GetSectionRecordBeforeTime(TimeSpan timeBefore);

        bool TryGetSpecificTracker(string trackerName, out ISpecificLapTracker specificLapTracker);

        bool TryGetSpecificTracker<T>(out T specificLapTracker);
    }
}
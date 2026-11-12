namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker
{
    using System;
    using DataModel.BasicProperties;

    public readonly struct SectionRecord
    {
        public static readonly SectionRecord EmptyRecord = new SectionRecord(-1, TimeSpan.Zero, TimeSpan.Zero, 0, 0, new Point3D(), 0);

        public SectionRecord(int sectionIndex, TimeSpan sessionTime, TimeSpan lapTime, double lapDistance, double totalDistance, Point3D worldPos, int lapNumber)
        {
            LapDistance = lapDistance;
            TotalDistance = totalDistance;
            LapTime = lapTime;
            WorldPos = worldPos;
            SessionTime = sessionTime;
            LapNumber = lapNumber;
            SectionIndex = sectionIndex;
        }

        public int SectionIndex { get; }

        public TimeSpan SessionTime { get; }

        public TimeSpan LapTime { get; }

        public double LapDistance { get; }

        public double TotalDistance { get; }

        public Point3D WorldPos { get; }

        public int LapNumber { get; }
    }
}
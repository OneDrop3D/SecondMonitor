namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap;

    public class LapData
    {
        public LapData(ILapInfo? lapInfo)
        {
            if (lapInfo == null)
            {
                return;
            }

            Completed = lapInfo.Completed;
            IsValid = lapInfo.Valid;
            LapNumber = lapInfo.LapNumber;
            LapEndPosition = lapInfo.LapEndPosition;
            LapEndPositionInClass = lapInfo.LapEndPositionInClass;
            LapGuid = lapInfo.LapGuid;
            IsPitLap = lapInfo.IsPitLap;
            LapTimeSeconds = lapInfo.LapTime.TotalSeconds;
            IsTelemetryDataAvailable = lapInfo.LapTelemetryInfo is { IsTelemetryPurged: false, TimedTelemetrySnapshots.Snapshots.Count : > 1 };
        }

        public bool Completed { get; }

        public bool IsValid { get; }

        public int LapNumber { get; }

        public int LapEndPosition { get; }
        public int LapEndPositionInClass { get; }

        public Guid LapGuid { get; }

        public bool IsPitLap { get; }
        public double LapTimeSeconds { get; }
        
        public bool IsTelemetryDataAvailable { get; }
    }
}

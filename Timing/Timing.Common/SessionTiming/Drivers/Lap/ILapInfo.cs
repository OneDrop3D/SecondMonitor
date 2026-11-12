namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using Telemetry;

    public interface ILapInfo
    {
        event EventHandler<SectorCompletedArgs> SectorCompletedEvent;
        event EventHandler<LapEventArgs> LapInvalidatedEvent;
        event EventHandler<LapEventArgs> LapCompletedEvent;

        bool Completed { get; }

        bool Valid { get; }

        bool IsPending { get;  }

        int LapNumber { get; }

        int LapEndPosition { get; }
        int LapEndPositionInClass { get; }

        Guid LapGuid { get; }

        LapCompletionMethod LapCompletionMethod { get; set; }

        bool IsPitLap { get; }

        bool IsPitEntryLap { get; }

        bool IsPitExitLap { get; }

        TimeSpan LapTime { get; }
        DriverTiming Driver { get; }
        SectorTiming Sector1 { get; }

        SectorTiming Sector2 { get; }

        SectorTiming Sector3 { get;  }

        TimeSpan CurrentlyValidProgressTime { get;  }

        LapTelemetryInfo LapTelemetryInfo { get;  }
        bool FirstLap { get; }
        bool InvalidBySim { get; set; }
        ILapInfo PreviousLap { get; }
        double CompletedDistance { get; }

        int StintNumber { get; }

        TimeSpan LapStartSessionTime { get; }

        public TimeSpan LapEndSessionTime { get; }
        LapInvalidationReasonKind LapInvalidationReasonKind { get; }
        bool IsSafetyCarLap { get; }

        bool UpdatePendingState(SimulatorDataSet set, DriverInfo driverInfo);
        void FinishLap(SimulatorDataSet dataSet, DriverInfo driverInfo);
        bool SwitchToPendingIfNecessary(SimulatorDataSet set, DriverInfo driverInfo);
        void InvalidateLap(LapInvalidationReasonKind driverDnf);
        void Tick(SimulatorDataSet dataSet, DriverInfo driverInfo);
        bool IsLapDataSane(SimulatorDataSet dataSet);
        void OverrideTime(TimeSpan overrideLapTime, bool forceValid);
    }
}
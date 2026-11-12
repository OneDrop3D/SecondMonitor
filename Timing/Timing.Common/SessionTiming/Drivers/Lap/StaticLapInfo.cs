namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using Drivers;
    using Telemetry;

    public class StaticLapInfo : ILapInfo
    {
        public StaticLapInfo(DriverTiming driver) : this(0, TimeSpan.Zero, false, null, 0, driver )
        {
        }

        public StaticLapInfo(int lapNumber, TimeSpan lapTime, bool firstLap, ILapInfo previousLap, double completedDistance, DriverTiming driver)
        {
            LapGuid = Guid.NewGuid();
            LapNumber = lapNumber;
            LapTime = lapTime;
            FirstLap = firstLap;
            PreviousLap = previousLap;
            CompletedDistance = completedDistance;
            Driver = driver;
            Sector1 = new SectorTiming(1, TimeSpan.Zero, this);
            Sector2 = new SectorTiming(2, TimeSpan.Zero, this);
            Sector3 = new SectorTiming(3, TimeSpan.Zero, this);
            LapTelemetryInfo = null;
        }

        //Implementing interface events, suppress warning
#pragma warning disable CS0067
        public event EventHandler<SectorCompletedArgs> SectorCompletedEvent;
        public event EventHandler<LapEventArgs> LapInvalidatedEvent;
        public event EventHandler<LapEventArgs> LapCompletedEvent;
#pragma warning restore CS0067

        public bool Completed { get; set; } = true;
        public bool Valid => true;
        public bool IsPending => false;
        public int LapNumber { get; }
        public int LapEndPosition { get; set; }
        public int LapEndPositionInClass { get; set; }
        public Guid LapGuid { get; }
        public LapCompletionMethod LapCompletionMethod { get; set; }
        public bool IsPitLap { get; set; }
        public bool IsPitEntryLap { get; set; }
        public bool IsPitExitLap { get; set; }
        public TimeSpan LapTime { get; set; }
        public DriverTiming Driver { get; }
        public SectorTiming Sector1 { get;  }
        public SectorTiming Sector2 { get;  }
        public SectorTiming Sector3 { get;  }
        public TimeSpan CurrentlyValidProgressTime => LapTime;
        public LapTelemetryInfo LapTelemetryInfo { get; }
        public bool FirstLap { get; set; }
        public bool InvalidBySim { get; set; }

        public ILapInfo PreviousLap { get; set; }
        public double CompletedDistance { get;  }

        public int StintNumber { get; set; }
        public TimeSpan LapStartSessionTime => TimeSpan.Zero;
        public TimeSpan LapEndSessionTime => TimeSpan.Zero;
        public LapInvalidationReasonKind LapInvalidationReasonKind => LapInvalidationReasonKind.None;
        public bool IsSafetyCarLap => false;

        public bool UpdatePendingState(SimulatorDataSet set, DriverInfo driverInfo)
        {
            return false;
        }

        public void FinishLap(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
        }

        public bool SwitchToPendingIfNecessary(SimulatorDataSet set, DriverInfo driverInfo)
        {
            return false;
        }

        public void InvalidateLap(LapInvalidationReasonKind driverDnf)
        {
        }

        public void Tick(SimulatorDataSet dataSet, DriverInfo driverInfo)
        {
        }

        public bool IsLapDataSane(SimulatorDataSet dataSet)
        {
            return true;
        }

        public void OverrideTime(TimeSpan overrideLapTime, bool forceValid)
        {
            LapTime = overrideLapTime;
        }
    }
}
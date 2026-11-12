namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using NLog;

    public class SectorTiming : IComparable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly TimeSpan _startTime;
        private TimeSpan _pendingStart;

        public SectorTiming(int sectorNumber, SimulatorDataSet simulatorData, ILapInfo lap) : this(sectorNumber, simulatorData.SessionInfo.SessionTime, lap)
        {
        }

        public SectorTiming(int sectorNumber, TimeSpan startTime, ILapInfo lap)
        {
            SectorNumber = sectorNumber;
            _startTime = startTime;
            Lap = lap;
        }

        public int SectorNumber { get; }

        public TimeSpan Duration { get; private set; } = TimeSpan.Zero;

        public ILapInfo Lap { get; }

        public bool IsFinished { get; private set; }

        public static bool operator <(SectorTiming s1, SectorTiming s2)
        {
            return s1.Duration < s2.Duration;
        }

        public static bool operator >(SectorTiming s1, SectorTiming s2)
        {
            return s1.Duration > s2.Duration;
        }

        public static bool operator <=(SectorTiming s1, SectorTiming s2)
        {
            return s1.Duration <= s2.Duration;
        }

        public static bool operator >=(SectorTiming s1, SectorTiming s2)
        {
            return s1.Duration >= s2.Duration;
        }

        public void Tick(SimulatorDataSet dataSet, DriverInfo driverTiming)
        {
            Duration = dataSet.SessionInfo.SessionTime - _startTime;
        }

        public void Finish(DriverInfo driverTiming, SimulatorDataSet dataSet)
        {
            Duration = PickTiming(driverTiming);
            if (Duration == TimeSpan.Zero && dataSet.SimulatorSourceInfo.SectorTimingSupport == DataInputSupport.SpOnly)
            {
                Duration = _pendingStart != TimeSpan.Zero ? _pendingStart : Lap.CurrentlyValidProgressTime;

                switch (SectorNumber)
                {
                    case 2:
                        Duration = Lap.Sector1 != null ? Duration - Lap.Sector1.Duration : TimeSpan.Zero;
                        break;
                    case 3:
                        Duration = Lap.Sector2 != null ? Duration - (Lap.Sector1.Duration + Lap.Sector2.Duration) : TimeSpan.Zero;
                        break;
                }
            }

            if (Duration.TotalSeconds <= 1)
            {
                logger.Warn($"Invalid Sector Time for Driver {driverTiming.DriverShortName}, Sector {SectorNumber}, Time Was {Duration.TotalSeconds}s");
                Duration = TimeSpan.Zero;
            }

            IsFinished = true;
        }

        public void SwitchToPending(TimeSpan sessionTime)
        {
            _pendingStart = Lap.CurrentlyValidProgressTime;
        }

        private TimeSpan PickTiming(DriverInfo driverInfo)
        {
            return PickTimingFormDriverInfo(driverInfo, SectorNumber);
        }

        public static TimeSpan PickTimingFormDriverInfo(DriverInfo driverInfo, int sectorNumber)
        {
            switch (sectorNumber)
            {
                case 1:
                    return driverInfo.Timing.LastSector1Time;
                case 2:
                    return driverInfo.Timing.LastSector2Time;
                case 3:
                    return driverInfo.Timing.LastSector3Time;
                default:
                    return TimeSpan.Zero;
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is SectorTiming))
            {
                return -1;
            }

            return Duration.CompareTo(((SectorTiming)obj).Duration);
        }
    }
}
namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;

    public class DriverData
    {
        public DriverData(SessionIdentification sessionIdentification, DriverTiming driverTiming)
        {
            SessionIdentification = sessionIdentification;
            DriverId = driverTiming.DriverId;
            DriverShortName = driverTiming.DriverShortName;
            DriverLongName = driverTiming.DriverLongName;
            IsPlayer = driverTiming.IsPlayer;
            Position = driverTiming.Position;
            PositionInClass = driverTiming.PositionInClass;
            CarName = driverTiming.CarName;
            CarClassName = driverTiming.CarClassName;
            CarClassId = driverTiming.CarClassId;
            CompletedLaps = driverTiming.CompletedLaps;
            IsInPits = driverTiming.InPits;
            PaceInSeconds = driverTiming.Pace.TotalSeconds;
            TotalDistanceTraveledMeters = driverTiming.TotalDistanceTraveled;
            IsLappedByPlayer = driverTiming.IsLappedByPlayer;
            IsLappingPlayer = driverTiming.IsLappingPlayer;
            DistanceToPlayerMeters = driverTiming.DistanceToPlayer;
            BestLap = new LapData(driverTiming.BestLap);
            PitCount = driverTiming.PitCount;
            CurrentLapPercentage = driverTiming.LapPercentage;
            IsLastLapBestLap = driverTiming.IsLastLapBestLap;
            IsLastLapBestClassSessionLap = driverTiming.IsLastLapBestClassSessionLap;
            IsActive = driverTiming.IsActive;
            IsLastSector1PersonalBest = driverTiming.IsLastSector1PersonalBest;
            IsLastSector2PersonalBest = driverTiming.IsLastSector2PersonalBest;
            IsLastSector3PersonalBest = driverTiming.IsLastSector3PersonalBest;

            IsLastSector1SessionBest = driverTiming.IsLastSector1SessionBest;
            IsLastSector2SessionBest = driverTiming.IsLastSector2SessionBest;
            IsLastSector3SessionBest = driverTiming.IsLastSector3SessionBest;

            IsLastSector1ClassSessionBest = driverTiming.IsLastSector1ClassSessionBest;
            IsLastSector2ClassSessionBest = driverTiming.IsLastSector2ClassSessionBest;
            IsLastSector3ClassSessionBest = driverTiming.IsLastSector3ClassSessionBest;

            CurrentLap = new LapData(driverTiming.CurrentLap);

            LastLap = new LapData(driverTiming.LastLap);

            IsLastLapBestSessionLap = driverTiming.IsLastLapBestSessionLap;
            Remark = driverTiming.Remark;

            SpeedKph = driverTiming.DriverInfo.Speed.InKph;

            TopSpeedKph = driverTiming.TopSpeed.InKph;

            Rating = driverTiming.Rating;
            ChampionshipPoints = driverTiming.ChampionshipPoints;
            ChampionshipPosition = driverTiming.ChampionshipPosition;
            IsLastLapTrackRecord = driverTiming.IsLastLapTrackRecord;
            GapToPlayerRelativeSeconds = driverTiming.GapToPlayerRelative.TotalSeconds;

            GapToPlayerAbsoluteSeconds = driverTiming.GapToPlayerAbsolute.TotalSeconds;

            GapToLeaderRelativeSeconds = driverTiming.GapToLeaderRelative.TotalSeconds;
            LapToPlayerDifference = driverTiming.LapToPlayerDifference;
            CurrentLapDistance = driverTiming.DriverInfo.LapDistance;
            IsSafetyCar = driverTiming.DriverInfo.IsSafetyCar;
            TeamName = driverTiming.DriverInfo.TeamName;
            CarRaceNumber = driverTiming.DriverInfo.CarRaceNumber;
            IsCarRaceNumberFilled = driverTiming.DriverInfo.IsCarRaceNumberFilled;
            WorldPosition = new Point3DData(driverTiming.DriverInfo.WorldPosition);
            IsCausingYellow = driverTiming.DriverInfo.IsCausingYellow;
            FinishStatus = (int)driverTiming.DriverInfo.FinishStatus;
            FinishStatusName = driverTiming.DriverInfo.FinishStatus.ToString();
        }

        public SessionIdentification SessionIdentification { get; }

        public string DriverId { get; }

        public string DriverShortName { get; }

        public string DriverLongName { get; }

        public bool IsPlayer { get; }

        public int Position { get; }

        public int PositionInClass { get; }

        public string CarName { get; }

        public string CarClassName { get; }

        public string CarClassId { get; }

        public int CompletedLaps { get; }

        public bool IsInPits { get; }

        public double PaceInSeconds { get; }

        public double TotalDistanceTraveledMeters { get; }

        public bool IsLappedByPlayer { get; }

        public bool IsLappingPlayer { get; }

        public double DistanceToPlayerMeters { get; }

        public LapData BestLap { get; }

        public int PitCount { get; }

        /*public PitStopInfo LastPitStop => _pitStopInfo.Count != 0 ? _pitStopInfo[_pitStopInfo.Count - 1] : null;

        public IReadOnlyCollection<PitStopInfo> PitStops => _pitStopInfo.AsReadOnly();*/

        public double CurrentLapPercentage { get; }

        public double CurrentLapDistance { get; }

        public bool IsLastLapBestLap { get; }

        public bool IsLastLapBestClassSessionLap { get; }

        public bool IsActive { get; }

        /*public SectorTiming BestSector1 { get; private }

        public SectorTiming BestSector2 { get; private }

        public SectorTiming BestSector3 { get; private }*/

        public bool IsLastSector1PersonalBest { get; }
        public bool IsLastSector2PersonalBest { get; }
        public bool IsLastSector3PersonalBest { get; }

        public bool IsLastSector1SessionBest { get; }
        public bool IsLastSector2SessionBest { get; }
        public bool IsLastSector3SessionBest { get; }

        public bool IsLastSector1ClassSessionBest { get; }
        public bool IsLastSector2ClassSessionBest { get; }
        public bool IsLastSector3ClassSessionBest { get; }

        public LapData CurrentLap { get; }

        public LapData LastLap { get; }

        public bool IsLastLapBestSessionLap { get; }
        public string Remark { get; }

        public double SpeedKph { get; }

        public double TopSpeedKph { get; }

        public int Rating { get; }
        public int ChampionshipPoints { get; }
        public int ChampionshipPosition { get; }
        public bool IsLastLapTrackRecord { get; }
        public double GapToPlayerRelativeSeconds { get; }

        public double GapToPlayerAbsoluteSeconds { get; }

        public double GapToLeaderRelativeSeconds { get; }
        public double LapToPlayerDifference { get; }
        public bool IsSafetyCar { get; }
        public string TeamName { get; }
        public int CarRaceNumber { get; }
        public bool IsCarRaceNumberFilled { get; }
        public Point3DData WorldPosition { get; }
        public bool IsCausingYellow { get; }

        public int FinishStatus { get; }

        public string FinishStatusName { get; }
    }
}

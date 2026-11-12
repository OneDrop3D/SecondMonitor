namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.MapView
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;

    public class MapViewDriverInfoFacade : IDriverInfo
    {
        public MapViewDriverInfoFacade(IDriverInfo parentInfo, int customPosition, string customName)
        {
            ParentInfo = parentInfo;
            DriverLongName = DriverShortName = DriverSessionId = customName;
            Position = customPosition;
        }

        public IDriverInfo ParentInfo { get; set; }

        public string DriverSessionId { get; private set; }
        public string DriverShortName { get; set; }
        public string DriverLongName { get; set; }

        public string CarName => ParentInfo.CarName;

        public string CarClassName => ParentInfo.CarClassName;

        public string CarClassId => ParentInfo.CarClassId;
        public bool IsSafetyCar => false;
        public string TeamName => string.Empty;
        public int CarRaceNumber => 0;
        public bool IsCarRaceNumberFilled => false;

        public int CompletedLaps => ParentInfo.CompletedLaps;

        public bool InPits => ParentInfo.InPits;

        public bool IsPlayer { get; set; }

        public int Position { get; set; }

        public int PositionInClass => Position;

        public bool CurrentLapValid => true;

        public double LapDistance => ParentInfo.LapDistance;

        public double TotalDistance => ParentInfo.TotalDistance;

        public double DistanceToPlayer => ParentInfo.DistanceToPlayer;

        public bool IsBeingLappedByPlayer => false;

        public bool IsLappingPlayer => false;

        public DriverFinishStatus FinishStatus => ParentInfo.FinishStatus;

        public CarInfo CarInfo => ParentInfo.CarInfo;

        public DriverTimingInfo Timing => ParentInfo.Timing;

        public Point3D WorldPosition => ParentInfo.WorldPosition;

        public DriverDebugInfo DriverDebugInfo => ParentInfo.DriverDebugInfo;

        public Velocity Speed => ParentInfo.Speed;
        public bool IsCausingYellow => ParentInfo.IsCausingYellow;
    }
}
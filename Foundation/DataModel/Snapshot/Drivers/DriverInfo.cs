namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    using System;
    using System.Diagnostics;
    using BasicProperties;
    using ProtoBuf;
    using Systems;

    [ProtoContract]
    [Serializable]
    [DebuggerDisplay("Driver Name: {DriverShortName}")]
    public sealed class DriverInfo : IDriverInfo
    {
        private string _carDisplayName;

        public DriverInfo()
        {
            WorldPosition = new Point3D();
            Speed = Velocity.FromMs(0);
            RatingInfo = new RatingInfo();
            DriverShortName = string.Empty;
            DriverLongName = string.Empty;
            DriverSessionId = string.Empty;
            CarClassId = string.Empty;
            CarClassName = string.Empty;
            CarName = string.Empty;
            TeamName = string.Empty;
            CarInfo = new CarInfo();
            StintTimeLeft = TimeSpan.Zero;
        }

        [ProtoMember(1)]
        public string DriverShortName { get; set; }

        [ProtoMember(2)]
        public string CarName { get; set; }

        [ProtoMember(3)]
        public string CarClassName { get; set; }

        [ProtoMember(4)]
        public string CarClassId { get; set; }

        [ProtoMember(31)]
        public bool IsSafetyCar { get; set; }

        [ProtoMember(27)]
        public string TeamName { get; set; }

        [ProtoMember(28)]
        public int CarRaceNumber { get; set; }

        [ProtoMember(29, IsRequired = true)]
        public bool IsCarRaceNumberFilled { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public int CompletedLaps { get; set; }

        [ProtoMember(25, IsRequired = true)]
        public bool InPits { get; set; }

        [ProtoMember(26, IsRequired = true)]
        public bool PitStopRequested { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public bool IsPlayer { get; set; }

        [ProtoMember(7, IsRequired = true)]
        public int Position { get; set; }

        [ProtoMember(8, IsRequired = true)]
        public int PositionInClass { get; set; }

        [ProtoMember(17, IsRequired = true)]
        public bool CurrentLapValid { get; set; }

        [ProtoMember(9, IsRequired = true)]
        public double LapDistance { get; set; }

        [ProtoMember(18, IsRequired = true)]
        public double TotalDistance { get; set; }

        [ProtoMember(19, IsRequired = true)]
        public double DistanceToPlayer { get; set; }

        [ProtoMember(20, IsRequired = true)]
        public bool IsBeingLappedByPlayer { get; set; } = false;

        [ProtoMember(21, IsRequired = true)]
        public bool IsLappingPlayer { get; set; } = false;

        [ProtoMember(22, IsRequired = true)]
        public DriverFinishStatus FinishStatus { get; set; } = DriverFinishStatus.Na;

        [ProtoMember(10)]
        public CarInfo CarInfo { get; set; }

        [ProtoMember(23, IsRequired = true)]
        public DriverTimingInfo Timing { get; set; } = new DriverTimingInfo();

        [ProtoMember(11, IsRequired = true)]
        public Point3D WorldPosition { get; set; }

        [ProtoMember(24, IsRequired = true)]
        public DriverDebugInfo DriverDebugInfo { get; } = new DriverDebugInfo();

        [ProtoMember(12, IsRequired = true)]
        public Velocity Speed { get; set; }

        [ProtoMember(13, IsRequired = true)]
        public bool IsCausingYellow { get; set; }

        [ProtoMember(14)]
        public string DriverSessionId { get; set; }

        [ProtoMember(15)]
        public string DriverLongName { get; set; }

        [ProtoMember(16)]
        public RatingInfo RatingInfo { get; set; }

        [ProtoMember(30)]
        public TimeSpan StintTimeLeft { get; set; }

        [ProtoMember(32)]
        public string CarDisplayName
        {
            get => string.IsNullOrWhiteSpace(_carDisplayName) ? CarName : _carDisplayName;
            set => _carDisplayName = value;
        }

        public double ComputeRelativeDistanceTo(DriverInfo otherDriver, double layoutLength)
        {
            if (FinishStatus == DriverFinishStatus.Dq || FinishStatus == DriverFinishStatus.Dnf ||
                FinishStatus == DriverFinishStatus.Dnq || FinishStatus == DriverFinishStatus.Dns)
            {
                return double.MaxValue;
            }

            double otherDriverLapDistance = otherDriver.LapDistance;

            double distanceToPlayer = otherDriverLapDistance - LapDistance;
            if (distanceToPlayer < -(layoutLength / 2))
            {
                distanceToPlayer += layoutLength;
            }

            if (distanceToPlayer > (layoutLength / 2))
            {
                distanceToPlayer -= layoutLength;
            }

            return distanceToPlayer;
        }

        public bool IsBeingLappedBy(DriverInfo otherDriver, double layoutLength)
        {
            return TotalDistance < (otherDriver.TotalDistance - (layoutLength * 0.5));
        }

        public bool IsLapping(DriverInfo otherDriver, double layoutLength)
        {
            return otherDriver.TotalDistance < (TotalDistance - (layoutLength * 0.5));
        }
    }
}

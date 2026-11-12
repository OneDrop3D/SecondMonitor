namespace SecondMonitor.DataModel.Snapshot
{
    using System;
    using BasicProperties;
    using ProtoBuf;
    using Systems;

    [Serializable]
    [ProtoContract]
    public sealed class SessionInfo
    {
        public SessionInfo()
        {
            SessionTime = TimeSpan.Zero;
            TrackInfo = new TrackInfo();
            PitWindow = new PitWindowInformation();
        }

        [ProtoMember(1, IsRequired = true)]
        public TimeSpan SessionTime { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public TrackInfo TrackInfo { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public bool IsActive { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public SessionType SessionType { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public SessionPhase SessionPhase { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public SessionLengthType SessionLengthType { get; set; } = SessionLengthType.Na;

        [ProtoMember(7, IsRequired = true)]
        public double SessionTimeRemaining { get; set; } = 0;

        [ProtoMember(8, IsRequired = true)]
        public int TotalNumberOfLaps { get; set; } = 0;

        [ProtoMember(9, IsRequired = true)]
        public int LeaderCurrentLap { get; set; }

        [ProtoMember(10, IsRequired = true)]
        public WeatherInfo WeatherInfo { get; set; } = new WeatherInfo();

        [ProtoMember(11, IsRequired = true)]
        public FlagKind ActiveFlags { get; set; }

        [ProtoMember(12, IsRequired = true)]
        public bool IsMultiClass { get; set; }

        [ProtoMember(13, IsRequired = true)]
        public bool IsMultiplayer { get; set; } = false;

        [ProtoMember(14, IsRequired = true)]
        public PitWindowInformation PitWindow { get; set; }

        [ProtoMember(15, IsRequired = true)]
        public SpectatingState SpectatingState { get; set; }

        [ProtoMember(16, IsRequired = true)]
        public SafetyCarState SafetyCarState { get; set; }

        public bool IsYellowFlagSituation => ActiveFlags.HasFlag(FlagKind.YellowSector1) || ActiveFlags.HasFlag(FlagKind.YellowSector2) || ActiveFlags.HasFlag(FlagKind.YellowSector3);

        public bool IsTimedSession => SessionLengthType is SessionLengthType.Time or SessionLengthType.TimeWithExtraLap;
    }
}

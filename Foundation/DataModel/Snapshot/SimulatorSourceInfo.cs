namespace SecondMonitor.DataModel.Snapshot
{
    using System;
    using System.Xml.Serialization;
    using Drivers;
    using ProtoBuf;

    [ProtoContract]
    [Serializable]
    public sealed class SimulatorSourceInfo
    {
        public SimulatorSourceInfo()
        {
            TelemetryInfo = new TelemetryInfo();
        }

        [XmlAttribute]
        [ProtoMember(2, IsRequired = true)]
        public bool HasLapTimeInformation { get; set; }

        [XmlAttribute]
        [ProtoMember(3, IsRequired = true)]
        public DataInputSupport SectorTimingSupport { get; set; } = DataInputSupport.None;

        [XmlAttribute]
        [ProtoMember(4, IsRequired = true)]
        // Some sims, like rFactor do not show a clear change in laps/lap status when the driver crosses the finish line in his out lap and moves to the hot lap. app needs to use alternative methods to detect this state
        public bool SimNotReportingEndOfOutLapCorrectly { get; set; }

        [XmlAttribute]
        [ProtoMember(5, IsRequired = true)]
        // Some sims, like r3e automatically complete the final lap after the playre crosses the line
        public bool AIInstantFinish { get; set; }

        [XmlAttribute]
        [ProtoMember(6, IsRequired = true)]
        // Some sims, like AC do not report out lap as invalid
        public bool OutLapIsValid { get; set; }

        [XmlAttribute]
        [ProtoMember(7, IsRequired = true)]
        // For some sims (i.e. AMS) it is difficult tu check if lap is valid. One of the methods to look if the sectors were updated. This flag allows this behavior.
        public bool InvalidateLapBySector { get; set; }

        [XmlAttribute]
        [ProtoMember(8, IsRequired = true)]
        // Some sims do not update all relevant information when lap changes. This flag forces a pending state when lap changes, allowing all relevant information to be updated properly.
        public bool ForceLapOverTime { get; set; }

        [XmlAttribute]
        [ProtoMember(9, IsRequired = true)]
        // Indicates if the sim has a global pool of tyre compounds (i.e. assetto corsa), or if each car has its own. The latter means that tyre compound for two cars can have different properties, even if the name is the same
        public bool GlobalTyreCompounds { get; set; }

        [XmlAttribute]
        [ProtoMember(10, IsRequired = true)]
        //Indicates that the world positions (x,y,z) provided by the sim are not valid.
        public bool WorldPositionInvalid { get; set; }

        [XmlAttribute]
        [ProtoMember(11, IsRequired = true)]
        //Indicates if the time is interpolated - i.e RF2 only refresh time every 200ms, between those 200ms the connector will interpolate the time
        public bool TimeInterpolated { get; set; }

        [XmlAttribute]
        [ProtoMember(12, IsRequired = true)]
        //Indicates that there is possibility of prolonged N/A Session Type between two valid session. I.e. in AMS when the player is in the pre-race screen, the game doesn't return a valid session type (i.e. race).
        //This affects i.e. rating,
        public bool NAStateBetweenSessions { get; set; }

        [XmlAttribute]
        [ProtoMember(13, IsRequired = true)]
        public GapInformationKind GapInformationProvided { get; set; }

        [ProtoMember(14, IsRequired = true)]
        [XmlAttribute]
        public bool HasRewindFunctionality { get; set; }

        //In some sims it might be required to the best lap times to be continuously checked and updated if needed. I.e. in F1 2019 this is needed because accelerated time is not exposed to the shared data.
        [ProtoMember(15, IsRequired = true)]
        [XmlAttribute]
        public bool OverrideBestLap { get; set; }

        //In some sims it may be preferred to estimate tyre wear up to light wear limit.
        [ProtoMember(16, IsRequired = true)]
        [XmlAttribute]
        public bool DetermineLapsUntilHeavyWearByLightWear { get; set; }

        [ProtoMember(1)]
        public TelemetryInfo TelemetryInfo { get; set; }

        private bool Equals(SimulatorSourceInfo other)
        {
            return HasLapTimeInformation == other.HasLapTimeInformation
                   && SectorTimingSupport == other.SectorTimingSupport
                   && SimNotReportingEndOfOutLapCorrectly == other.SimNotReportingEndOfOutLapCorrectly
                   && AIInstantFinish == other.AIInstantFinish
                   && OutLapIsValid == other.OutLapIsValid
                   && InvalidateLapBySector == other.InvalidateLapBySector
                   && ForceLapOverTime == other.ForceLapOverTime
                   && GlobalTyreCompounds == other.GlobalTyreCompounds
                   && WorldPositionInvalid == other.WorldPositionInvalid
                   && TimeInterpolated == other.TimeInterpolated
                   && NAStateBetweenSessions == other.NAStateBetweenSessions
                   && GapInformationProvided == other.GapInformationProvided
                   && HasRewindFunctionality == other.HasRewindFunctionality
                   && DetermineLapsUntilHeavyWearByLightWear == other.DetermineLapsUntilHeavyWearByLightWear
                   && OverrideBestLap == other.OverrideBestLap;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || ((obj is SimulatorSourceInfo other) && (Equals(other)));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = HasLapTimeInformation.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)SectorTimingSupport;
                hashCode = (hashCode * 397) ^ SimNotReportingEndOfOutLapCorrectly.GetHashCode();
                hashCode = (hashCode * 397) ^ AIInstantFinish.GetHashCode();
                hashCode = (hashCode * 397) ^ OutLapIsValid.GetHashCode();
                hashCode = (hashCode * 397) ^ InvalidateLapBySector.GetHashCode();
                hashCode = (hashCode * 397) ^ ForceLapOverTime.GetHashCode();
                hashCode = (hashCode * 397) ^ GlobalTyreCompounds.GetHashCode();
                hashCode = (hashCode * 397) ^ WorldPositionInvalid.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeInterpolated.GetHashCode();
                hashCode = (hashCode * 397) ^ NAStateBetweenSessions.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)GapInformationProvided;
                hashCode = (hashCode * 397) ^ HasRewindFunctionality.GetHashCode();
                hashCode = (hashCode * 397) ^ OverrideBestLap.GetHashCode();
                hashCode = (hashCode * 397) ^ DetermineLapsUntilHeavyWearByLightWear.GetHashCode();
                hashCode = (hashCode * 397) ^ (TelemetryInfo != null ? TelemetryInfo.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
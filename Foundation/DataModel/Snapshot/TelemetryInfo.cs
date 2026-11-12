namespace SecondMonitor.DataModel.Snapshot
{
    using System;
    using System.Xml.Serialization;
    using ProtoBuf;

    [ProtoContract]
    [Serializable]
    public sealed class TelemetryInfo
    {
        [XmlAttribute]
        [ProtoMember(1)]
        public bool RequiresDistanceInterpolation { get; set; }

        [XmlAttribute]
        [ProtoMember(2)]
        public bool RequiresPositionInterpolation { get; set; }

        [XmlAttribute]
        [ProtoMember(3)]
        public bool ContainsSuspensionVelocity { get; set; }

        [XmlAttribute]
        [ProtoMember(4)]
        public bool ContainsSuspensionTravel { get; set; }

        [XmlAttribute]
        [ProtoMember(5)]
        public bool ContainsOptimalTemperatures { get; set; }

        [XmlAttribute]
        [ProtoMember(6)]
        public bool ContainsSlipInformation { get; set; }

        [XmlAttribute]
        [ProtoMember(7)]
        public bool ContainsWheelRps { get; set; }

        [XmlAttribute]
        [ProtoMember(8)]
        public bool ContainsTyreLoad { get; set; }

        [XmlAttribute]
        [ProtoMember(9)]
        public bool ContainsOverallDownforce { get; set; }

        [XmlAttribute]
        [ProtoMember(10)]
        public bool ContainsFrontRearDownforce { get; set; }

        [XmlAttribute]
        [ProtoMember(11)]
        public bool IsSuspensionTravelInverted { get; set; }
    }
}
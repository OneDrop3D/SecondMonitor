namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class Wheels
    {
        public Wheels()
        {
            FrontRight = new WheelInfo(WheelKind.FrontRight);
            FrontLeft = new WheelInfo(WheelKind.FrontLeft);
            RearRight = new WheelInfo(WheelKind.RearRight);
            RearLeft = new WheelInfo(WheelKind.RearLeft);
        }

        [XmlIgnore]
        [JsonIgnore]
        public WheelInfo[] AllWheels => new WheelInfo[] { FrontLeft, FrontRight, RearLeft, RearRight };

        [ProtoMember(1)]
        public WheelInfo FrontLeft { get; set; }

        [ProtoMember(2)]
        public WheelInfo FrontRight { get; set; }

        [ProtoMember(3)]
        public WheelInfo RearLeft { get; set; }

        [ProtoMember(4)]
        public WheelInfo RearRight { get; set; }

        [XmlAttribute]
        [ProtoMember(5)]
        public bool IsRideHeightFilled { get; set; }

        public bool IsWheelFront(WheelInfo wheelInfo)
        {
            return wheelInfo == FrontLeft || wheelInfo == FrontRight;
        }
    }
}
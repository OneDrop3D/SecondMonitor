namespace SecondMonitor.Telemetry.TelemetryManagement.Settings
{
    using System;
    using System.Xml.Serialization;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using ProtoBuf;

    [ProtoContract]
    public class CarPropertiesDto
    {
        public CarPropertiesDto()
        {
            FrontLeftTyre = new WheelPropertiesDto()
            {
                SprintStiffnessPerMm = Force.GetFromNewtons(330),
            };
            FrontRightTyre = new WheelPropertiesDto()
            {
                SprintStiffnessPerMm = Force.GetFromNewtons(330),
            };
            RearLeftTyre = new WheelPropertiesDto()
            {
                SprintStiffnessPerMm = Force.GetFromNewtons(225),
            };
            RearRightTyre = new WheelPropertiesDto()
            {
                SprintStiffnessPerMm = Force.GetFromNewtons(225),
            };
            SteeringLock = Angle.GetFromDegrees(15);
        }

        [XmlAttribute]
        [ProtoMember(1)]
        public string CarName { get; set; }

        [XmlAttribute]
        [ProtoMember(2)]
        public string Simulator { get; set; }

        [ProtoMember(3)]
        public Angle SteeringLock { get; set; }

        [ProtoMember(4)]
        public WheelPropertiesDto FrontLeftTyre { get; set; }

        [ProtoMember(5)]
        public WheelPropertiesDto FrontRightTyre { get; set; }

        [ProtoMember(6)]
        public WheelPropertiesDto RearLeftTyre { get; set; }

        [ProtoMember(7)]
        public WheelPropertiesDto RearRightTyre { get; set; }

        public WheelPropertiesDto GetPropertiesByWheelKind(WheelKind wheelKind)
        {
            switch (wheelKind)
            {
                case WheelKind.FrontLeft:
                    return FrontLeftTyre;
                case WheelKind.FrontRight:
                    return FrontRightTyre;
                case WheelKind.RearLeft:
                    return RearLeftTyre;
                case WheelKind.RearRight:
                    return RearRightTyre;
                default:
                    throw new ArgumentOutOfRangeException(nameof(wheelKind), wheelKind, null);
            }
        }
    }
}
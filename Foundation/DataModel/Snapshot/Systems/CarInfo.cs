namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;
    using System.Xml.Serialization;
    using BasicProperties;
    using BasicProperties.Units;
    using Drivers;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class CarInfo
    {
        [ProtoMember(1)]
        public Wheels WheelsInfo { get; set; } = new();

        [ProtoMember(2)]
        public OilInfo OilSystemInfo { get; set; } = new();

        [ProtoMember(3)]
        public FuelInfo FuelSystemInfo { get; set; } = new();

        [ProtoMember(4)]
        public WaterInfo WaterSystemInfo { get; set; } = new();

        [ProtoMember(5)]
        public Acceleration Acceleration { get; set; } = new();

        [ProtoMember(6)]
        public Distance FrontHeight { get; set; } = Distance.ZeroDistance;

        [ProtoMember(7)]
        public Distance RearHeight { get; set; } = Distance.ZeroDistance;

        [ProtoMember(8)]
        [XmlAttribute]
        public string CurrentGear { get; set; } = string.Empty;

        [ProtoMember(9, IsRequired = true)]
        [XmlAttribute]
        public int EngineRpm { get; set; } = 0;

        [ProtoMember(10)]
        public Pressure TurboPressure { get; set; } = Pressure.Zero;

        [ProtoMember(11, IsRequired = true)]
        public bool SpeedLimiterEngaged { get; set; }

        [ProtoMember(12)]
        public CarDamageInformation CarDamageInformation { get; set; } = new();

        [ProtoMember(22)]
        public DrsSystem DrsSystem { get; set; } = new();

        [ProtoMember(21)]
        public BoostSystem BoostSystem { get; set; } = new();

        [ProtoMember(13)]
        public Force OverallDownForce { get; set; } = new();

        [ProtoMember(14)]
        public Force FrontDownForce { get; set; } = new();

        [ProtoMember(15)]
        public Force RearDownForce { get; set; } = new();

        [ProtoMember(16)]
        public Angle FrontRollAngle { get; set; } = new();

        [ProtoMember(17)]
        public Angle RearRollAngle { get; set; } = new();

        [ProtoMember(18)]
        public Orientation WorldOrientation { get; set; } = new();

        [ProtoMember(19)]
        public Power EnginePower { get; set; } = new();

        [ProtoMember(20)]
        public Torque EngineTorque { get; set; } = new();

        [ProtoMember(23, IsRequired = true)]
        public HeadLightsStatus HeadLightsStatus { get; set; }

        [ProtoMember(24)]
        public SafetySystemInfo AbsInfo { get; set; } = new();

        [ProtoMember(25)]
        public SafetySystemInfo TcInfo { get; set; } = new();

        [ProtoMember(26)]
        public HybridSystem HybridSystem { get; set; } = new();
    }
}

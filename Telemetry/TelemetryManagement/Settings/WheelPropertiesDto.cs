namespace SecondMonitor.Telemetry.TelemetryManagement.Settings
{
    using DataModel.BasicProperties;
    using ProtoBuf;

    [ProtoContract]
    public class WheelPropertiesDto
    {
        public WheelPropertiesDto()
        {
            BumpTransition = Velocity.FromMs(0.025);
            ReboundTransition = Velocity.FromMs(-0.025);
            IdealCamber = Angle.GetFromDegrees(0);
            NeutralSuspensionTravel = Distance.FromMeters(0);
            SprintStiffnessPerMm = Force.GetFromNewtons(150);
        }

        [ProtoMember(1)]
        public Velocity BumpTransition { get; set; }

        [ProtoMember(2)]
        public Velocity ReboundTransition { get; set; }

        [ProtoMember(3)]
        public Angle IdealCamber { get; set; }

        [ProtoMember(4)]
        public Force SprintStiffnessPerMm { get; set; }

        [ProtoMember(5)]
        public Distance NeutralSuspensionTravel { get; set; }
    }
}
namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;
    using BasicProperties;
    using ProtoBuf;

    [ProtoContract]
    public class BoostSystem
    {
        [ProtoMember(1, IsRequired = true)]
        public BoostStatus BoostStatus { get; set; } = BoostStatus.UnAvailable;

        [ProtoMember(2, IsRequired = true)]
        public TimeSpan CooldownTimer { get; set; } = TimeSpan.Zero;

        [ProtoMember(3, IsRequired = true)]
        public TimeSpan TimeRemaining { get; set; } = TimeSpan.Zero;

        [ProtoMember(4, IsRequired = true)]
        public int ActivationsRemaining { get; set; } = -1;
    }
}
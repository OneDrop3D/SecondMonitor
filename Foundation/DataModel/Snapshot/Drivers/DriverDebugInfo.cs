namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    using System;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class DriverDebugInfo
    {
        [ProtoMember(1, IsRequired = true)]
        public double DistanceToPits { get; set; }
    }
}
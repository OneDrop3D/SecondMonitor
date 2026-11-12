namespace SecondMonitor.Remote.Models
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class DatagramPayload
    {
        public const string Version4 = "SecondMonitor_RemoteVersion_4";

        [ProtoMember(1, IsRequired = true)]
        public bool ContainsPlayersTiming { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public bool ContainsOtherDriversTiming { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public bool ContainsSimulatorSourceInfo { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public DatagramPayloadKind PayloadKind { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public string Source { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public InputInfo InputInfo { get; set; }

        [ProtoMember(7, IsRequired = true)]
        public SessionInfo SessionInfo { get; set; }

        [ProtoMember(8, IsRequired = true)]
        public DriverInfo[] DriversInfo { get; set; }

        [ProtoMember(9, IsRequired = true)]
        public DriverInfo PlayerInfo { get; set; }

        [ProtoMember(10, IsRequired = true)]
        public DriverInfo LeaderInfo { get; set; }

        [ProtoMember(11, IsRequired = true)]
        public SimulatorSourceInfo SimulatorSourceInfo { get; set; } = new SimulatorSourceInfo();
    }
}
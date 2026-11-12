namespace SecondMonitor.Debug
{
    using ProtoBuf;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    [ProtoContract]
    public class DataPacket
    {
        [ProtoMember(1, IsRequired = true)]
        public string Source { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public InputInfo InputInfo { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public SessionInfo SessionInfo { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public DriverInfo[] DriversInfo { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public DriverInfo PlayerInfo { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public DriverInfo LeaderInfo { get; set; }

        [ProtoMember(7, IsRequired = true)]
        public SimulatorSourceInfo SimulatorSourceInfo { get; set; } = new();
    }
}
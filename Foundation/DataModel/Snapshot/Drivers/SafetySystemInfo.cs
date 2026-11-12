namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    [Serializable]
    public class SafetySystemInfo
    {
        [ProtoMember(1)]
        public bool IsAvailable { get; set; } = false;
        [ProtoMember(2)]
        public bool IsActive { get; set; } = false;
        [ProtoMember(3)]
        public string Remark { get; set; } = string.Empty;
    }
}
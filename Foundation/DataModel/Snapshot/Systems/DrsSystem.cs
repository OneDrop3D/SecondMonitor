namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;
    using BasicProperties;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class DrsSystem
    {
        public DrsSystem()
        {
            DrsActivationLeft = -1;
        }

        [ProtoMember(1, IsRequired = true)]
        public DrsStatus DrsStatus { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public int DrsActivationLeft { get; set; }
    }
}
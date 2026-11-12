namespace SecondMonitor.Debug
{
    using ProtoBuf;

    [ProtoContract]
    public class DataHeader
    {
        [ProtoMember(1, IsRequired = true)]
        public int UpdateInterval { get; set; }        
    }
}
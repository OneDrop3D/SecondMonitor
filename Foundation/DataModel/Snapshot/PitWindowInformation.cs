namespace SecondMonitor.DataModel.Snapshot
{
    using System;
    using System.Xml.Serialization;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class PitWindowInformation
    {
        [XmlAttribute]
        [ProtoMember(1, IsRequired = true)]
        public PitWindowState PitWindowState { get; set; }

        [XmlAttribute]
        [ProtoMember(2, IsRequired = true)]
        public double PitWindowStart { get; set; }

        [XmlAttribute]
        [ProtoMember(3, IsRequired = true)]
        public double PitWindowEnd { get; set; }
    }
}
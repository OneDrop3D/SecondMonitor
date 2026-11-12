namespace SecondMonitor.DataModel.Snapshot
{
    using System;
    using System.Xml.Serialization;
    using BasicProperties;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class TrackInfo
    {
        public TrackInfo()
        {
            TrackName = string.Empty;
            TrackLayoutName = string.Empty;
            LayoutLength = Distance.ZeroDistance;
        }

        [ProtoMember(1, IsRequired = true)]
        public string TrackName { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string TrackLayoutName { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public Distance LayoutLength { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public string TrackFullName => string.IsNullOrEmpty(TrackLayoutName) ? TrackName : $"{TrackName}-{TrackLayoutName}";
    }
}
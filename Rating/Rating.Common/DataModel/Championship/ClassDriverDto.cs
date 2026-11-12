namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class ClassDriverDto : ParticipantDto
    {
        [XmlAttribute]
        public Guid DriverGlobalKey { get; set; }

        [XmlAttribute]
        public string DriverName { get; set; }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public string TeamName { get; set; }
    }
}

namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System.Xml.Serialization;

    public class ParticipantDto
    {
        [XmlAttribute]
        public int TotalPoints { get; set; }

        [XmlAttribute]
        public int Position { get; set; }

        [XmlAttribute]
        public bool IsPlayer { get; set; }
    }
}
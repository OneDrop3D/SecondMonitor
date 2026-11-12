namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System.Xml.Serialization;

    public class TeamDto : ParticipantDto
    {
        [XmlAttribute]
        public string TeamName { get; set; }
    }
}

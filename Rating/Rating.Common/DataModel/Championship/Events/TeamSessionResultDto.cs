namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System.Xml.Serialization;

    public class TeamSessionResultDto : ParticipantSessionResultDto
    {
        [XmlAttribute]
        public string TeamName { get; set; }
    }
}
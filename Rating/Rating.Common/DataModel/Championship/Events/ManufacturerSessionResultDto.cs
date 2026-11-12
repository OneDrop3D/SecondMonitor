namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System.Xml.Serialization;

    public class ManufacturerSessionResultDto : ParticipantSessionResultDto
    {
        [XmlAttribute]
        public string ManufacturerName { get; set; }
    }
}
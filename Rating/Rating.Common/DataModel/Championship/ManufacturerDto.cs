namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System.Xml.Serialization;

    public class ManufacturerDto : ParticipantDto
    {
        [XmlAttribute]
        public string ManufacturerName { get; set; }
    }
}
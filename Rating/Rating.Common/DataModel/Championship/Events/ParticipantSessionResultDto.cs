namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System.Xml.Serialization;

    public class ParticipantSessionResultDto
    {
        [XmlAttribute]
        public bool IsPlayer { get; set; }

        [XmlAttribute]
        public int FinishPosition { get; set; }

        [XmlAttribute]
        public int TotalPoints { get; set; }

        [XmlAttribute]
        public int PointsGain { get; set; }

        [XmlAttribute]
        public int BeforeEventPosition { get; set; }

        [XmlAttribute]
        public int AfterEventPosition { get; set; }

        [XmlAttribute]
        public bool SkippedEvent { get; set; }

        [XmlIgnore]
        public int PositionGained => BeforeEventPosition - AfterEventPosition;
    }
}
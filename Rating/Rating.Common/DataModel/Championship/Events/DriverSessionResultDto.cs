namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System;
    using System.Xml.Serialization;

    public class DriverSessionResultDto : ParticipantSessionResultDto
    {
        [XmlAttribute]
        public Guid DriverGuid { get; set; }

        [XmlAttribute]
        public string DriverName { get; set; }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public string ClassName { get; set; }

        [XmlAttribute]
        public string ClassId { get; set; }

        [XmlAttribute]
        public int ClassFinishPosition { get; set; }

        [XmlAttribute]
        public string TeamName { get; set; }

        [XmlAttribute]
        public bool WasFastestLap { get; set; }
    }
}
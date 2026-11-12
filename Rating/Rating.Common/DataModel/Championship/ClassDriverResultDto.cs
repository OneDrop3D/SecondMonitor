namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Xml.Serialization;

    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class ClassDriverResultDto : ParticipantSessionResultDto
    {
        [XmlAttribute]
        public Guid DriverGlobalKey { get; set; }

        [XmlAttribute]
        public string DriverName { get; set; }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public string TeamName { get; set; }

        [XmlAttribute]
        public bool WasFastestLap { get; set; }
    }
}

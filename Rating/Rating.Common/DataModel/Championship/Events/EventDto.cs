namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    public class EventDto
    {
        [XmlAttribute]
        public string EventName { get; set; }

        [XmlAttribute]
        public string TrackName { get; set; }

        [XmlAttribute]
        public bool IsMysteryTrack { get; set; }

        [XmlAttribute]
        public bool IsTrackNameExact { get; set; }

        [XmlAttribute]
        public EventStatus EventStatus { get; set; }

        public List<SessionDto> Sessions { get; set; } = new();

        [XmlAttribute]
        public bool IsEventDateDefined { get; set; }

        [XmlAttribute]
        public DateTime EventDate { get; set; }
    }
}
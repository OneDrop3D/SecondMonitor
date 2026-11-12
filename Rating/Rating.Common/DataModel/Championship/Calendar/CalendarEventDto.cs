namespace SecondMonitor.Rating.Common.DataModel.Championship.Calendar
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class CalendarEventDto
    {
        [XmlAttribute]
        public string TrackName { get; set; }

        [XmlAttribute]
        public string EventName { get; set; }

        [XmlAttribute]
        public bool IsTrackNameExact { get; set; }

        [XmlAttribute]
        public bool IsMysteryTrack { get; set; }

        public DateTime? EventDate { get; set; }
    }
}
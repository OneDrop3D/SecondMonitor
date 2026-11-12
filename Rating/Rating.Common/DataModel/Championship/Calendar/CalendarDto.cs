namespace SecondMonitor.Rating.Common.DataModel.Championship.Calendar
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    public class CalendarDto
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Simulator { get; set; }

        [XmlAttribute]
        public bool IsChampionshipDatesFilled { get; set; }

        [XmlAttribute]
        public DateTime StartDate { get; set; }

        [XmlAttribute]
        public DateTime EndDate { get; set; }

        public List<CalendarEventDto> Events { get; set; }
    }
}
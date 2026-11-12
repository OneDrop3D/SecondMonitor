namespace SecondMonitor.DataModel.TrackRecords
{
    using System;
    using System.Globalization;
    using System.Xml.Serialization;
    using BasicProperties;

    public class RecordEntryDto
    {
        [XmlIgnore]
        public DateTime RecordDate { get; set; }

        [XmlAttribute]
        public string RecordDateTime
        {
            get => RecordDate.ToString("O");
            set => RecordDate = DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
        }

        [XmlIgnore]
        public TimeSpan LapTime { get; set; }

        [XmlAttribute]
        public double LapTimeSeconds
        {
            get => LapTime.TotalSeconds;
            set => LapTime = TimeSpan.FromSeconds(value);
        }

        [XmlAttribute]
        public double AverageSpeedInKph
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool HasSpeed => AverageSpeedInKph > 0;

        [XmlAttribute]
        public SessionType SessionType { get; set; }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public string CarDisplayName { get; set; }

        [XmlAttribute]
        public string CarClass { get; set; }

        [XmlAttribute]
        public string PlayerName { get; set; }

        [XmlAttribute]
        public bool IsPlayer { get; set; }
    }
}
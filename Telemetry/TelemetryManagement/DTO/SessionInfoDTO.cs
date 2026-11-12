namespace SecondMonitor.Telemetry.TelemetryManagement.DTO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "SessionInfo")]
    public class SessionInfoDto
    {
        public SessionInfoDto()
        {
            SessionTransientData = new SessionTransientData();
        }

        [XmlIgnore]
        public DateTime SessionRunDateTime { get; set; }

        [XmlAttribute]
        public string SessionDateTime
        {
            get => SessionRunDateTime.ToString("O");
            set => SessionRunDateTime = DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
        }

        [XmlIgnore]
        public SessionTransientData SessionTransientData { get; }

        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Simulator { get; set; }

        [XmlAttribute]
        public string TrackName { get; set; }

        [XmlAttribute]
        public string LayoutName { get; set; }

        [XmlAttribute]
        public double LayoutLength { get; set; }

        [XmlAttribute]
        public string PlayerName { get; set; }

        [XmlAttribute]
        public string SessionType { get; set; }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public string Remarks { get; set; }

        [XmlAttribute]
        public string SessionCustomName { get; set; }

        public List<LapSummaryDto> LapsSummary { get; set; }
    }
}
namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System.Xml.Serialization;
    using SecondMonitor.DataModel.BasicProperties;

    public class SessionDto
    {
        [XmlAttribute]
        public string DistanceDescription { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public SessionLengthType ActualLengthType { get; set; }

        [XmlAttribute]
        public bool IsDistanceBased { get; set; }

        [XmlAttribute]
        public double DistanceInMeters { get; set; }

        /// <summary>
        /// Actual Session Length it was run with, seconds for time sessions, laps for lap sessions
        /// </summary>
        [XmlAttribute]
        public double ActualSessionLength { get; set; }

        [XmlAttribute]
        public bool IsSessionLengthFilled { get; set; }

        public SessionResultDto SessionResult { get; set; }

        public void FillSessionLength(SessionLengthType sessionLengthType, double sessionLength)
        {
            IsSessionLengthFilled = true;
            ActualLengthType = sessionLengthType;
            ActualSessionLength = sessionLength;
        }
    }
}
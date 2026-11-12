namespace SecondMonitor.Telemetry.TelemetryApplication.Settings.DTO
{
    using System.Xml.Serialization;

    public class AggregatedChartSettingsDto
    {
        public AggregatedChartSettingsDto()
        {
            HideNotSelectedPoints = true;
        }

        [XmlAttribute]
        public StintRenderingKind StintRenderingKind { get; set; }

        [XmlAttribute]
        public bool HideNotSelectedPoints { get; set; }
    }
}
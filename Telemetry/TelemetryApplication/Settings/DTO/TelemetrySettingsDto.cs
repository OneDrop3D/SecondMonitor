namespace SecondMonitor.Telemetry.TelemetryApplication.Settings.DTO
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using TelemetryManagement.Settings;
    using Workspace;
    using Workspaces.Providers.BuildIn;

    [XmlRoot(ElementName = "TelemetrySettings")]
    public class TelemetrySettingsDto
    {
        public TelemetrySettingsDto()
        {
            AggregatedChartSettings = new AggregatedChartSettingsDto();
            SelectedWorkspaceGuid = DefaultWorkspaceProvider.Guid;
            CustomWorkspaces = new List<Workspace>();
            CustomChartSets = new List<ChartSet>();
        }

        [XmlAttribute]
        public XAxisKind XAxisKind { get; set; }

        [XmlElement(ElementName = "AggregatedChartSettings")]
        public AggregatedChartSettingsDto AggregatedChartSettings { get; set; }

        [XmlAttribute(AttributeName = "SelectedWorkspaceGuid")]
        public string SelectedWorkspaceGuidString
        {
            get => SelectedWorkspaceGuid.ToString();
            set => SelectedWorkspaceGuid = Guid.Parse(value);
        }

        [XmlIgnore]
        public Guid SelectedWorkspaceGuid { get; set; }

        public List<Workspace> CustomWorkspaces { get; set; }

        public List<ChartSet> CustomChartSets { get; set; }

        public CarsProperties CarsProperties { get; set; }
    }
}
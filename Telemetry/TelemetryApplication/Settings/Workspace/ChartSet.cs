namespace SecondMonitor.Telemetry.TelemetryApplication.Settings.Workspace
{
    using System;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using SecondMonitor.ViewModels.Layouts;

    [Serializable]
    public class ChartSet
    {
        public ChartSet()
        {
            Guid = Guid.NewGuid();
        }

        [XmlIgnore]
        [JsonIgnore]
        public Guid Guid { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public virtual bool IsBuildIn => false;

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Favourite { get; set; }

        public LayoutDescription LayoutDescription { get; set; }

        [XmlAttribute]
        public string GuidAsString
        {
            get => Guid.ToString("N");
            set => Guid = Guid.Parse(value);
        }
    }
}
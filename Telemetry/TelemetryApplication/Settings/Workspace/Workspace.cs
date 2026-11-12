namespace SecondMonitor.Telemetry.TelemetryApplication.Settings.Workspace
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [Serializable]
    public class Workspace
    {
        public Workspace()
        {
            ChartSetsGuid = new List<string>();
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
        public bool IsFavourite { get; set; }

        public List<string> ChartSetsGuid { get; set; }

        [XmlAttribute]
        public string GuidAsString
        {
            get => Guid.ToString("N");
            set => Guid = Guid.Parse(value);
        }
    }
}
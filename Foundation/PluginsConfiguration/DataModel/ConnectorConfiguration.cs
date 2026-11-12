namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System.Xml.Serialization;
    public class ConnectorConfiguration
    {
        [XmlAttribute]
        public string ConnectorName { get; set; }

        [XmlAttribute]
        public bool IsEnabled { get; set; }
    }
}
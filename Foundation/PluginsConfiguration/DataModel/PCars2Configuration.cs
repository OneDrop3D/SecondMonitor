namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class PCars2Configuration
    {
        public PCars2Configuration()
        {
            Port = 5606;
        }

        [XmlAttribute]
        public int Port { get; set; }
    }
}
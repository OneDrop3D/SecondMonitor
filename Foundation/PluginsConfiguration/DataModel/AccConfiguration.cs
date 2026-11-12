namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class AccConfiguration
    {
        public AccConfiguration()
        {
            ConnectionPassword = "asd";
            ConnectionCommandPassword = string.Empty;
            Port = 9000;
            PoolInterval = 200;
            CustomName = string.Empty;
        }

        [XmlAttribute]
        public string ConnectionPassword { get; set; }

        [XmlAttribute]
        public string ConnectionCommandPassword { get; set; }

        [XmlAttribute]
        public int Port { get; set; }

        [XmlAttribute]
        public int PoolInterval { get; set; }

        [XmlAttribute]
        public string CustomName { get; set; }
    }
}
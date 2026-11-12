namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class ForzaMotorsportConfiguration
    {
        public ForzaMotorsportConfiguration()
        {
            Port = 20127;
            DriverName = "Player";
        }

        [XmlAttribute]
        public int Port { get; set; }

        [XmlAttribute]
        public string DriverName { get; set; }
    }
}
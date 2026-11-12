namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class F12019Configuration
    {
        public F12019Configuration()
        {
            Port = 20777;
            MyTeamName = "My Team";
        }

        [XmlAttribute]
        public int Port { get; set; }

        [XmlAttribute]
        public string MyTeamName { get; set; }
    }
}
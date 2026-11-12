namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System;
    using System.Xml.Serialization;

    public class RemoteConfiguration
    {
        public RemoteConfiguration()
        {
            BroadcastLimitSettings = new BroadcastLimitSettings()
            {
                IsEnabled = false,
                MinimumPackageInterval = 30,
                PlayerTimingPackageInterval = 200,
                OtherDriversTimingPackageInterval = 1000,
            };
        }

        [XmlAttribute]
        [Obsolete("IpAddress is obsolete. Use HostAddress instead")]
        public string IpAddress { get; set; }

        [XmlAttribute]
        public string HostAddress { get; set; }

        [XmlAttribute]
        public bool IsFindInLanEnabled { get; set; }

        [XmlAttribute]
        public int Port { get; set; }

        public BroadcastLimitSettings BroadcastLimitSettings { get; set; }

        [XmlAttribute]
        public bool IsMultiClientsEnabled { get; set; }
    }
}
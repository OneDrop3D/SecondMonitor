namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System.Xml.Serialization;

    public class BroadcastLimitSettings
    {
        [XmlAttribute]
        public bool IsEnabled { get; set; }

        [XmlAttribute]
        public int MinimumPackageInterval { get; set; }

        [XmlAttribute]
        public int PlayerTimingPackageInterval { get; set; }

        [XmlAttribute]
        public int OtherDriversTimingPackageInterval { get; set; }
    }
}
namespace SecondMonitor.PluginsConfiguration.Common.DataModel
{
    using System.Collections.Generic;

    public class PluginsConfiguration
    {
        public RemoteConfiguration RemoteConfiguration { get; set; }
        public F12019Configuration F12019Configuration { get; set; } = new();
        public List<PluginConfiguration> PluginsConfigurations { get; set; } = new();
        public List<ConnectorConfiguration> ConnectorConfigurations { get; set; } = new();
        public ForzaMotorsportConfiguration ForzaMotorsportConfiguration { get; set; } = new();
        public PCars2Configuration PCars2Configurations { get; set; } = new();
        public AccConfiguration AccConfiguration { get; set; } = new();
        public Ams2Configuration Ams2Configuration { get; set; } = new();
    }
}
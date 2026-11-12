namespace SecondMonitor.Remote.Debug
{
    using System;

    using SecondMonitor.PluginsConfiguration.Common.Controller;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;
    internal class MockedPluginSettingsProvider : IPluginSettingsProvider
    {
        public PluginsConfiguration PluginConfiguration
        {
            get;
            set;
        }

        public RemoteConfiguration RemoteConfiguration { get; set; }
            = new RemoteConfiguration();

        public F12019Configuration F12019Configuration
        {
            get;
            set;
        }

        public ForzaMotorsportConfiguration ForzaMotorsportConfiguration
        {
            get;
            set;
        }

        public PCars2Configuration PCars2Configurations
        {
            get;
            set;
        }

        public Ams2Configuration Ams2Configuration { get; set; }

        public AccConfiguration AccConfiguration
        {
            get;
            set;
        }

        public bool TryIsConnectorEnabled(string connectorName, out bool isEnabled)
        {
            throw new NotSupportedException();
        }

        public void SetConnectorEnabled(string connectorName, bool isConnectorEnabled)
        {
            throw new NotSupportedException();
        }

        public bool TryIsPluginEnabled(string pluginName, out bool isEnabled)
        {
            isEnabled = true;
            return true;
        }

        public void SetPluginEnabled(string pluginName, bool isPluginEnabled)
        {
            throw new NotSupportedException();
        }

        public void SaveConfiguration(PluginsConfiguration pluginsConfiguration)
        {
            throw new NotSupportedException();
        }
    }
}
namespace SecondMonitor.PluginsConfiguration.Common.Controller
{
    using System.Linq;
    using DataModel;
    using Repository;

    internal class PluginsSettingsProvider : IPluginSettingsProvider
    {
        private readonly IPluginConfigurationRepository _pluginConfigurationRepository;

        public PluginsSettingsProvider(IPluginConfigurationRepository pluginConfigurationRepository)
        {
            _pluginConfigurationRepository = pluginConfigurationRepository;
            PluginConfiguration = _pluginConfigurationRepository.LoadOrCreateDefault();
        }

        public RemoteConfiguration RemoteConfiguration => PluginConfiguration.RemoteConfiguration;
        public F12019Configuration F12019Configuration => PluginConfiguration.F12019Configuration;
        public ForzaMotorsportConfiguration ForzaMotorsportConfiguration => PluginConfiguration.ForzaMotorsportConfiguration;
        public PCars2Configuration PCars2Configurations => PluginConfiguration.PCars2Configurations;
        public AccConfiguration AccConfiguration => PluginConfiguration.AccConfiguration;
        public Ams2Configuration Ams2Configuration => PluginConfiguration.Ams2Configuration;
        public PluginsConfiguration PluginConfiguration { get; private set; }
        public bool TryIsConnectorEnabled(string connectorName, out bool isEnabled)
        {
            ConnectorConfiguration connectorConfiguration = PluginConfiguration.ConnectorConfigurations.FirstOrDefault(x => x.ConnectorName == connectorName);
            if (connectorConfiguration == null)
            {
                isEnabled = false;
                return false;
            }

            isEnabled = connectorConfiguration.IsEnabled;
            return true;
        }

        public void SetConnectorEnabled(string connectorName, bool isConnectorEnabled)
        {
            ConnectorConfiguration connectorConfiguration = PluginConfiguration.ConnectorConfigurations.FirstOrDefault(x => x.ConnectorName == connectorName);
            if (connectorConfiguration == null)
            {
                connectorConfiguration = new ConnectorConfiguration()
                {
                    ConnectorName = connectorName
                };
                PluginConfiguration.ConnectorConfigurations.Add(connectorConfiguration);
            }

            connectorConfiguration.IsEnabled = isConnectorEnabled;
            _pluginConfigurationRepository.Save(PluginConfiguration);
        }

        public bool TryIsPluginEnabled(string pluginName, out bool isEnabled)
        {
            PluginConfiguration pluginConfiguration = PluginConfiguration.PluginsConfigurations.FirstOrDefault(x => x.PluginName == pluginName);
            if (pluginConfiguration == null)
            {
                isEnabled = false;
                return false;
            }

            isEnabled = pluginConfiguration.IsEnabled;
            return true;
        }

        public void SetPluginEnabled(string pluginName, bool isPluginEnabled)
        {
            PluginConfiguration pluginConfiguration = PluginConfiguration.PluginsConfigurations.FirstOrDefault(x => x.PluginName == pluginName);
            if (pluginConfiguration == null)
            {
                pluginConfiguration = new PluginConfiguration()
                {
                    PluginName = pluginName
                };
                PluginConfiguration.PluginsConfigurations.Add(pluginConfiguration);
            }

            pluginConfiguration.IsEnabled = isPluginEnabled;
            _pluginConfigurationRepository.Save(PluginConfiguration);
        }

        public void SaveConfiguration(PluginsConfiguration pluginsConfiguration)
        {
            PluginConfiguration = pluginsConfiguration;
            _pluginConfigurationRepository.Save(PluginConfiguration);
        }
    }
}
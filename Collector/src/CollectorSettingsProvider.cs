namespace SecondMonitor.Collector
{
    using System.Linq;
    using SecondMonitor.PluginsConfiguration.Common.Controller;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;

    /// <summary>
    /// Réglages attendus par les connecteurs et l'empaquetage réseau de SecondMonitor, construits en mémoire
    /// à partir de collecteur.json (pas de fichier XML, pas de registre).
    /// </summary>
    internal sealed class CollectorSettingsProvider : IPluginSettingsProvider
    {
        public CollectorSettingsProvider(CollectorSettings settings)
        {
            var (_, minPackage, player, others) = settings.Rates;
            PluginConfiguration = new PluginsConfiguration
            {
                RemoteConfiguration = new RemoteConfiguration
                {
                    HostAddress = string.Empty,
                    Port = settings.Port,
                    IsMultiClientsEnabled = true,
                    BroadcastLimitSettings = new BroadcastLimitSettings
                    {
                        // Toujours actif : sans limite, SecondMonitor enverrait tout le plateau 60 fois par seconde.
                        IsEnabled = true,
                        MinimumPackageInterval = minPackage,
                        PlayerTimingPackageInterval = player,
                        OtherDriversTimingPackageInterval = others,
                    },
                },
            };
        }

        public PluginsConfiguration PluginConfiguration { get; private set; }

        public RemoteConfiguration RemoteConfiguration => PluginConfiguration.RemoteConfiguration;

        public F12019Configuration F12019Configuration => PluginConfiguration.F12019Configuration;

        public ForzaMotorsportConfiguration ForzaMotorsportConfiguration => PluginConfiguration.ForzaMotorsportConfiguration;

        public PCars2Configuration PCars2Configurations => PluginConfiguration.PCars2Configurations;

        public Ams2Configuration Ams2Configuration => PluginConfiguration.Ams2Configuration;

        public AccConfiguration AccConfiguration => PluginConfiguration.AccConfiguration;

        public bool TryIsConnectorEnabled(string connectorName, out bool isEnabled)
        {
            ConnectorConfiguration c = PluginConfiguration.ConnectorConfigurations.FirstOrDefault(x => x.ConnectorName == connectorName);
            isEnabled = c?.IsEnabled ?? true;
            return c != null;
        }

        public void SetConnectorEnabled(string connectorName, bool isConnectorEnabled)
        {
            ConnectorConfiguration c = PluginConfiguration.ConnectorConfigurations.FirstOrDefault(x => x.ConnectorName == connectorName);
            if (c == null)
            {
                PluginConfiguration.ConnectorConfigurations.Add(new ConnectorConfiguration { ConnectorName = connectorName, IsEnabled = isConnectorEnabled });
            }
            else
            {
                c.IsEnabled = isConnectorEnabled;
            }
        }

        public bool TryIsPluginEnabled(string pluginName, out bool isEnabled)
        {
            isEnabled = true;
            return true;
        }

        public void SetPluginEnabled(string pluginName, bool isPluginEnabled)
        {
        }

        public void SaveConfiguration(PluginsConfiguration pluginsConfiguration) => PluginConfiguration = pluginsConfiguration;
    }
}

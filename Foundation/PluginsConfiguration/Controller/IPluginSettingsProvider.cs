namespace SecondMonitor.PluginsConfiguration.Common.Controller
{
    using DataModel;

    public interface IPluginSettingsProvider
    {
        PluginsConfiguration PluginConfiguration { get; }

        RemoteConfiguration RemoteConfiguration { get; }
        F12019Configuration F12019Configuration { get; }
        ForzaMotorsportConfiguration ForzaMotorsportConfiguration { get; }
        PCars2Configuration PCars2Configurations { get; }
        Ams2Configuration Ams2Configuration { get; }
        AccConfiguration AccConfiguration { get; }

        bool TryIsConnectorEnabled(string connectorName, out bool isEnabled);

        void SetConnectorEnabled(string connectorName, bool isConnectorEnabled);

        bool TryIsPluginEnabled(string pluginName, out bool isEnabled);
        void SetPluginEnabled(string pluginName, bool isPluginEnabled);
        void SaveConfiguration(PluginsConfiguration pluginsConfiguration);
    }
}
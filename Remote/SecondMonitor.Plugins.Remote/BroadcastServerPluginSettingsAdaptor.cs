namespace SecondMonitor.Plugins.Remote
{
    using PluginsConfiguration.Common.Controller;

    internal class BroadcastServerPluginSettingsAdaptor : IBroadcastServerConfiguration
    {
        private IPluginSettingsProvider _pluginSettingsProvider;

        public BroadcastServerPluginSettingsAdaptor(IPluginSettingsProvider pluginSettingsProvider)
        {
            _pluginSettingsProvider = pluginSettingsProvider;
        }

        public int Port => _pluginSettingsProvider.RemoteConfiguration.Port;
    }
}
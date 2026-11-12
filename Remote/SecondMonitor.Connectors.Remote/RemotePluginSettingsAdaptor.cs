namespace SecondMonitor.Connectors.Remote
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using LiteNetLib;
    using PluginsConfiguration.Common.Controller;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;

    public class RemotePluginSettingsAdaptor : IRemoteConnectorConfiguration
    {
        private readonly Lazy<IPluginSettingsProvider> _pluginSettingsProvider;
        public RemotePluginSettingsAdaptor(Lazy<IPluginSettingsProvider> pluginSettingsProvider)
        {
            _pluginSettingsProvider = pluginSettingsProvider;
        }

        private RemoteConfiguration RemoteConfiguration => _pluginSettingsProvider.Value.RemoteConfiguration;

        public int? FindInLanPort => RemoteConfiguration.IsFindInLanEnabled ? RemoteConfiguration.Port : (int?)null;

        public bool IsMultiClientEnabled => RemoteConfiguration.IsMultiClientsEnabled;

        public IList<IPEndPoint> ServerPeers => string.IsNullOrWhiteSpace(RemoteConfiguration.HostAddress)
            ? new List<IPEndPoint>()
            : new List<IPEndPoint>()
            {
                NetUtils.MakeEndPoint(RemoteConfiguration.HostAddress, RemoteConfiguration.Port)
            };
    }
}
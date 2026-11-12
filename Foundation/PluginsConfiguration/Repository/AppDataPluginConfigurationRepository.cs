namespace SecondMonitor.PluginsConfiguration.Common.Repository
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using DataModel;

    using NLog;

    public class AppDataPluginConfigurationRepository : IPluginConfigurationRepository
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SecondMonitor\\plugins.xml");

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly XmlSerializer _xmlSerializer;

        public AppDataPluginConfigurationRepository()
        {
            _xmlSerializer = new XmlSerializer(typeof(PluginsConfiguration));
        }

        public PluginsConfiguration LoadOrCreateDefault()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return CreateDefault();
                }

                using (FileStream file = File.Open(SettingsPath, FileMode.Open))
                {
                    return _xmlSerializer.Deserialize(file) as PluginsConfiguration;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                return CreateDefault();
            }
        }

        public void Save(PluginsConfiguration pluginsConfiguration)
        {
            using (FileStream file = File.Exists(SettingsPath) ? File.Open(SettingsPath, FileMode.Truncate) : File.Create(SettingsPath))
            {
                _xmlSerializer.Serialize(file, pluginsConfiguration);
            }
        }

        private PluginsConfiguration CreateDefault()
        {
            return new PluginsConfiguration()
            {
                RemoteConfiguration = new RemoteConfiguration()
                {
                    HostAddress = string.Empty,
                    IsFindInLanEnabled = false,
                    Port = 52642
                }
            };
        }
    }
}
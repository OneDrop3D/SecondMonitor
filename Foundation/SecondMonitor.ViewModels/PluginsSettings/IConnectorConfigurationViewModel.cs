namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;
    public interface IConnectorConfigurationViewModel : IViewModel<ConnectorConfiguration>
    {
        string ConnectorName { get; set; }
        
        bool IsEnabled { get; set; }
    }
}
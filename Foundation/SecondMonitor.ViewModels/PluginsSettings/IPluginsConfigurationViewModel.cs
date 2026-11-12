namespace SecondMonitor.ViewModels.PluginsSettings
{
    using System.Collections.Generic;
    using PluginsConfiguration.Common.DataModel;

    public interface IPluginsConfigurationViewModel : IViewModel<PluginsConfiguration>
    {
        IRemoteConfigurationViewModel RemoteConfigurationViewModel { get; }

        IReadOnlyCollection<IPluginConfigurationViewModel> PluginConfigurations { get; }

        IReadOnlyCollection<IConnectorConfigurationViewModel> ConnectorConfigurations { get; }

        F12019ConfigurationViewModel F12019ConfigurationViewModel { get; }

        ForzaMotorsportConfigurationViewModel ForzaMotorsportConfigurationViewModel { get; }

        PCars2ConfigurationViewModel PCars2ConfigurationViewModel { get; }

        AccConfigurationViewModel AccConfigurationViewModel { get; }

        Ams2ConfigurationViewModel Ams2ConfigurationViewModel { get; }
    }
}
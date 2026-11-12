namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;

    public interface IRemoteConfigurationViewModel : IViewModel<RemoteConfiguration>
    {
        string HostAddress { get; set; }

        bool IsFindInLanEnabled { get; set; }

        int Port { get; set; }

        bool IsMultiClientEnabled { get; set; }

        IBroadcastLimitSettingsViewModel BroadcastLimitSettingsViewModel { get; }
    }
}
namespace SecondMonitor.PluginsConfiguration.Application.Controllers
{
    using System.Threading.Tasks;
    using System.Windows;
    using Common.Controller;
    using Common.Repository;
    using Contracts.Commands;
    using Foundation.Connectors;
    using PluginManager.Core;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.PluginsSettings;
    using ViewModels;

    public class PluginsConfigurationController : IPluginConfigurationController
    {
        private readonly IPluginSettingsProvider _pluginSettingsProvider;
        private readonly IPluginConfigurationRepository _pluginConfigurationRepository;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly Window _mainWindow;
        private readonly ISecondMonitorPlugin[] _allPlugins;
        private readonly IRawTelemetryConnector[] _telemetryConnectors;

        private IPluginsSettingsWindowViewModel _pluginsSettingsWindowViewModel;

        public PluginsConfigurationController(IPluginSettingsProvider pluginSettingsProvider, IPluginConfigurationRepository pluginConfigurationRepository, ISecondMonitorPlugin[] plugins, IRawTelemetryConnector[] connectors, IViewModelFactory viewModelFactory, Window mainWindow)
        {
            _pluginSettingsProvider = pluginSettingsProvider;
            _pluginConfigurationRepository = pluginConfigurationRepository;
            _allPlugins = plugins;
            _telemetryConnectors = connectors;
            _viewModelFactory = viewModelFactory;
            _mainWindow = mainWindow;
        }

        public async Task StartControllerAsync()
        {
            await Task.Run(() =>
            {
                InitializeConnectorsList();
                InitializePluginsList();
                InitializeViewModels();
            });
            _mainWindow.DataContext = _pluginsSettingsWindowViewModel;
        }

        private void InitializeViewModels()
        {
            _pluginsSettingsWindowViewModel = _viewModelFactory.Create<IPluginsSettingsWindowViewModel>();
            IPluginsConfigurationViewModel pluginsConfigurationViewModel = _viewModelFactory.Create<IPluginsConfigurationViewModel>();
            pluginsConfigurationViewModel.FromModel(_pluginConfigurationRepository.LoadOrCreateDefault());
            _pluginsSettingsWindowViewModel.SaveCommand = new RelayCommand(SaveAndClose);
            _pluginsSettingsWindowViewModel.CloseCommand = new RelayCommand(Close);
            _pluginsSettingsWindowViewModel.PluginsConfigurationViewModel = pluginsConfigurationViewModel;
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }

        private void SaveAndClose()
        {
            _pluginConfigurationRepository.Save(_pluginsSettingsWindowViewModel.PluginsConfigurationViewModel.SaveToNewModel());
            Close();
        }

        private void Close()
        {
            _mainWindow.Close();
        }

        private void InitializePluginsList()
        {
            foreach (ISecondMonitorPlugin secondMonitorPlugin in _allPlugins)
            {
                if (!_pluginSettingsProvider.TryIsPluginEnabled(secondMonitorPlugin.PluginName, out _))
                {
                    _pluginSettingsProvider.SetPluginEnabled(secondMonitorPlugin.PluginName, secondMonitorPlugin.IsEnabledByDefault);
                }
            }
        }

        private void InitializeConnectorsList()
        {
            foreach (IRawTelemetryConnector connector in _telemetryConnectors)
            {
                if (!_pluginSettingsProvider.TryIsConnectorEnabled(connector.Name, out _))
                {
                    _pluginSettingsProvider.SetConnectorEnabled(connector.Name, connector.IsEnabledByDefault);
                }
            }
        }
    }
}
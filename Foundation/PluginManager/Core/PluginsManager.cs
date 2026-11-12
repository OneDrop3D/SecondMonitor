namespace SecondMonitor.PluginManager.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using DataModel.Snapshot;
    using Foundation.Connectors;

    using NLog;
    using PluginsConfiguration.Common.Controller;
    using ViewModels;
    using ViewModels.Controllers;
    using ViewModels.Factory;

    public class PluginsManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISecondMonitorPlugin[] _plugins;
        private readonly List<ISecondMonitorPlugin> _runningPlugins;
        private readonly IPluginSettingsProvider _pluginSettingsProvider;
        private readonly List<ISimulatorDataSetVisitor> _visitors;
        private readonly IRawTelemetryConnector[] _connectors;
        private readonly AutoUpdateController _autoUpdateController;
        private readonly ConnectorViewModel _connectorViewModel;

        private IRawTelemetryConnector _activeConnector;
        private SimulatorDataSet _oldDataSet;

        public PluginsManager(IRawTelemetryConnector[] connectors, ISimulatorDataSetVisitor[] dataVisitors, ISecondMonitorPlugin[] plugins, IPluginSettingsProvider pluginSettingsProvider, AutoUpdateController autoUpdateController, IViewModelFactory viewModelFactory)
        {
            _plugins = plugins;
            _connectors = connectors;
            _visitors = dataVisitors.ToList();
            _runningPlugins = new List<ISecondMonitorPlugin>(_plugins.Length);
            _pluginSettingsProvider = pluginSettingsProvider;
            _autoUpdateController = autoUpdateController;
            _connectorViewModel = viewModelFactory.Create<ConnectorViewModel>();
        }

        private static void LogSimulatorDataSet(SimulatorDataSet dataSet)
        {
            /*using var stringWriter = new System.IO.StringWriter();
            var serializer = new XmlSerializer(typeof(SimulatorDataSet));
            serializer.Serialize(stringWriter, dataSet);

            string serializedDataSet = stringWriter.ToString();
            Logger.Info($"Simulator set\\n: {serializedDataSet}");*/
        }

        private async void Connector_Disconnected(object sender, EventArgs e)
        {
            if (_activeConnector != sender)
            {
                return;
            }

            if (_activeConnector == null)
            {
                return;
            }

            await _activeConnector.FinnishConnectorAsync();

            Logger.Info("Connector Disconnected: " + _activeConnector.GetType());
            _activeConnector.DataLoaded -= OnDataLoaded;
            _activeConnector.SessionStarted -= OnSessionStarted;
            _activeConnector.Disconnected -= Connector_Disconnected;
            _activeConnector.DisplayMessage -= ActiveConnectorOnDisplayMessage;
            _activeConnector = null;
            _connectorViewModel.UnBindPreviousConnector();
            RaiseSessionStartedEvent(new SimulatorDataSet("Not Connected"));
        }

        public async Task Start()
        {
            Logger.Info("-----------------------Application Started------------------------------------");
            /*AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;*/
            try
            {
                while (true)
                {
                    if (_activeConnector == null)
                    {
                        await ConnectLoop();
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /*private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = args.Name.Split(",").First() +".dll";
            if (!File.Exists(assemblyName))
            {
                throw new FileNotFoundException(args.Name);
            }

            Assembly assembly = Assembly.LoadFrom(assemblyName);
            if (assembly.FullName == args.Name)
            {
                return assembly;
            }

            throw new FileNotFoundException(args.Name);
        }*/

        private async Task ConnectLoop()
        {
            var enabledConnectors = _connectors.Where(IsConnectorEnabled).ToList();

            if (enabledConnectors.Count == 0)
            {
                MessageBox.Show("No enabled connectors loaded. Please enabled some of the existing connectors.", "No connectors", MessageBoxButtons.OK);
                Environment.Exit(1);
            }

            await Task.Delay(500).ConfigureAwait(false);
            while (true)
            {
                foreach (var connector in enabledConnectors)
                {
                    connector.DisplayMessage += ActiveConnectorOnDisplayMessage;
                    if (connector.TryConnect())
                    {
                        Logger.Info("Connector Connected: " + connector.GetType());
                        _activeConnector = connector;
                        _activeConnector.DataLoaded += OnDataLoaded;
                        _activeConnector.SessionStarted += OnSessionStarted;
                        _activeConnector.Disconnected += Connector_Disconnected;
                        _activeConnector.StartConnectorLoop();
                        _connectorViewModel.BindConnector(_activeConnector);
                        return;
                    }

                    connector.DisplayMessage -= ActiveConnectorOnDisplayMessage;
                }

                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        private async void ActiveConnectorOnDisplayMessage(object sender, MessageArgs messageArgs)
        {
            await Task.WhenAll(_runningPlugins.Select(x => x.OnDisplayMessage(sender, messageArgs)));
        }

        public async Task InitializePlugins()
        {
            var enabledPlugins = _plugins.Where(IsPluginEnabled).ToList();

            if (enabledPlugins.Count == 0)
            {
                MessageBox.Show("No enabled plugins loaded. Please enabled some of the existing plugins.", "No plugins", MessageBoxButtons.OK);
                Environment.Exit(1);
            }

            _autoUpdateController.CheckForUpdate();

            foreach (ISecondMonitorPlugin plugin in enabledPlugins)
            {
                Logger.Info("Running plugin " + plugin.GetType());
                plugin.PluginManager = this;
            }

            var pluginsTasks = enabledPlugins.Select(async x =>
            {
                await x.RunPlugin();
                _runningPlugins.Add(x);
            });

            try
            {
                await Task.WhenAll(pluginsTasks);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error pluggins");
            }
        }

        public async Task DeletePlugin(ISecondMonitorPlugin plugin, List<Exception> experiencedExceptions)
        {
            lock (_runningPlugins)
            {
                Logger.Info("Plugin " + plugin.GetType() + " closed");
                _runningPlugins.Remove(plugin);

                experiencedExceptions.ForEach(e => Logger.Error(e));

                bool allDaemons = _runningPlugins.Aggregate(true, (current, activePlugin) => current && activePlugin.IsDaemon);
                if (!allDaemons)
                {
                    return;
                }
            }

            if (_activeConnector != null)
            {
                await _activeConnector.FinnishConnectorAsync();
            }

            Logger.Info("------------------------------All plugins closed - application exiting-------------------------------\n\n\n");
            Environment.Exit(0);
            //Application.Exit(new CancelEventArgs(true));
        }

        private void OnDataLoaded(object sender, DataEventArgs args)
        {
            _visitors.ForEach(x => args.Data.Accept(x));
            RaiseDataLoadedEvent(args.Data);
        }

        private bool IsPluginEnabled(ISecondMonitorPlugin plugin)
        {
            if (_pluginSettingsProvider.TryIsPluginEnabled(plugin.PluginName, out bool isEnabled))
            {
                Logger.Info($"Plugin {plugin.PluginName} is Enabled: {isEnabled}");
                return isEnabled;
            }

            _pluginSettingsProvider.SetPluginEnabled(plugin.PluginName, plugin.IsEnabledByDefault);
            Logger.Info($"Plugin {plugin.PluginName} is Enabled: {plugin.IsEnabledByDefault}");
            return plugin.IsEnabledByDefault;
        }

        private bool IsConnectorEnabled(IRawTelemetryConnector connector)
        {
            if (_pluginSettingsProvider.TryIsConnectorEnabled(connector.Name, out bool isEnabled))
            {
                Logger.Info($"Plugin {connector.Name} is Enabled: {isEnabled}");
                return isEnabled;
            }

            _pluginSettingsProvider.SetConnectorEnabled(connector.Name, connector.IsEnabledByDefault);
            Logger.Info($"Plugin {connector.Name} is Enabled: {connector.IsEnabledByDefault}");
            return connector.IsEnabledByDefault;
        }

        private void OnSessionStarted(object sender, DataEventArgs args)
        {
            _visitors.ForEach(x => x.Reset());
            RaiseSessionStartedEvent(args.Data);
        }

        private async void RaiseSessionStartedEvent(SimulatorDataSet data)
        {
            try
            {
                Logger.Info("New Session starting " + data.SessionInfo.SessionType);

                /*if (_oldDataSet != null)
                {
                    Logger.Info("Old set:");
                    LogSimulatorDataSet(_oldDataSet);
                }
                
    
                _oldDataSet = data;
                Logger.Info("New set:");
                LogSimulatorDataSet(_oldDataSet);*/
                await Task.WhenAll(_runningPlugins.ToList().Select(x => x.OnSessionStarted(data)));
            }
            catch (TaskCanceledException ex)
            {
                Logger.Error(ex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                LogSimulatorDataSet(data);
            }
        }

        private async void RaiseDataLoadedEvent(SimulatorDataSet data)
        {
            try
            {
                _oldDataSet = data;
                await Task.WhenAll(_runningPlugins.ToList().Select(x => x.OnDataLoaded(data)));
            }
            catch (TaskCanceledException ex)
            {
                Logger.Error(ex);
            }
            catch (Exception)
            {
                LogSimulatorDataSet(_oldDataSet);
                throw;
            }
        }
    }
}
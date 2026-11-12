namespace SecondMonitor.Remote.Debug
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Ninject;
    using Ninject.Parameters;
    using Plugins.Remote;
    using Plugins.Remote.Controllers;
    using Plugins.Remote.ViewModels;
    using Bootstrapping;
    using DataModel.Snapshot;

    using PluginManager.Core;
    using PluginsConfiguration.Common.Controller;
    using Simulation;
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var raceSession = new RaceSession();
            
            var kernel = StandardKernelFactory.CreateNew();
            try
            {
                kernel.LoadCore();
                var mockedSettingsProvider = new MockedPluginSettingsProvider();

                kernel.Rebind<IPluginSettingsProvider>().ToConstant(mockedSettingsProvider);

                kernel.Rebind<IBroadcastServerConfiguration>().To<MockedBroadcastServerPluginConfiguration>();
                kernel.Rebind<IServerOverviewViewModel>().To<ServerOverviewViewModel>();
                kernel.Bind<DataBroadcasterPluginController>().ToSelf();

                var startingPort = 52642;

                ISecondMonitorPlugin[] plugins = new ISecondMonitorPlugin[raceSession.TrackedDrivers.Count];

                for (var i = 0; i < raceSession.TrackedDrivers.Count; i++)
                {
                    var serverPort = startingPort++;

                    var broadcastServerPluginConfiguration = new MockedBroadcastServerPluginConfiguration(serverPort);
                    var serverOverviewViewModel = kernel.Get<IServerOverviewViewModel>();

                    var boradcastServerPluginConfigurationArgument = new ConstructorArgument("broadcastServerConfiguration", broadcastServerPluginConfiguration);
                    var serverOverviewViewModelArgument = new ConstructorArgument("serverOverviewViewModel", serverOverviewViewModel);
                    var broadcastServerController = kernel.Get<IBroadCastServerController>(boradcastServerPluginConfigurationArgument, serverOverviewViewModelArgument);

                    var plugin = new DataBroadcasterPluginController(serverOverviewViewModel, broadcastServerController, broadcastServerPluginConfiguration);
                    plugins[i] = plugin;
                }

                var pluginsManager = kernel.Get<PluginsManager>(new ConstructorArgument("plugins", plugins));
                await pluginsManager.InitializePlugins();

                await Task.Delay(2000);

                var keyValuePairs = raceSession.TrackedDrivers.Select(x => x.Name)
                    .Zip(plugins, (driverName, server) => new KeyValuePair<string, ISecondMonitorPlugin>(driverName, server))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                await TestingThreadExecutor(keyValuePairs);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Environment.Exit(1);
            }
        }

        private async Task TestingThreadExecutor(Dictionary<string, ISecondMonitorPlugin> plugins)
        {
            RaceSession raceSessionSimulation = null;
            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(2000));
                raceSessionSimulation = raceSessionSimulation?.CreateFollowUpSession() ?? new RaceSession();

                foreach (var driver in raceSessionSimulation.TrackedDrivers)
                {
                    var simulatorDataSet = raceSessionSimulation.CreateSimulatorDataSetForDriver(driver, SessionPhase.Countdown);
                    await plugins[driver.Name].OnSessionStarted(simulatorDataSet);
                }

                long sleepTime = 16;
                var stopwatch = Stopwatch.StartNew();

                while (raceSessionSimulation.Entries.First(x => x.Position == 1).CompletedLaps <= raceSessionSimulation.Laps)
                {
                    var simulationStep = (double)(sleepTime + stopwatch.ElapsedMilliseconds) / 1000;
                    stopwatch = Stopwatch.StartNew();

                    raceSessionSimulation.StepSimulation(simulationStep);

                    foreach (var driver in raceSessionSimulation.TrackedDrivers)
                    {
                        var simulatorDataSet = raceSessionSimulation.CreateSimulatorDataSetForDriver(driver, SessionPhase.Green);
                        await plugins[driver.Name].OnDataLoaded(simulatorDataSet);
                    }

                    stopwatch.Stop();
                    sleepTime = Math.Max(15L - stopwatch.ElapsedMilliseconds, 0L);

                    await Task.Delay(TimeSpan.FromMilliseconds(sleepTime));
                }
            }
        }
    }
}
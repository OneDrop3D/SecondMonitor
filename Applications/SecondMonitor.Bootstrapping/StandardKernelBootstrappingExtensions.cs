namespace SecondMonitor.Bootstrapping
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Foundation.Connectors;
    using Ninject;
    using Ninject.Extensions.Conventions;
    using NLog;
    using PluginManager.Core;

    public static class StandardKernelBootstrappingExtensions
    {
        private static readonly string[] AssembliesToIgnore = { "rFactor2SharedMemoryMapPlugin64.dll", "rFactorSharedMemoryMap.dll", "ACInternalMemoryReader.dll", 
            "WebView2Loader.dll", "libSkiaSharp.dll", "libHarfBuzzSharp.dll" };
        private static readonly DirectoryInfo ConnectorsDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Connectors"));
        private static readonly DirectoryInfo PluginsDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"));

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void LoadCore(this IKernel kernel)
        {
            kernel.Load(
                "Contracts.dll",
                "DataModel.dll",
                "Plugin*.dll",
                "Rating*.dll",
                "SecondMonitor*.dll",
                "SimdataManagement.dll",
                "Telemetry*.dll",
                "Timing*.dll");
        }

        public static void LoadConnectors(this IKernel kernel)
        {
            var connectorDirectories = ConnectorsDir.EnumerateDirectories();

            foreach (var connectorDirectory in connectorDirectories)
            {
                Logger.Info($"Loading connector: {connectorDirectory.Name}");
                var assemblies = LoadAssembliesFromDirectory(connectorDirectory);
                kernel.Load(assemblies);

                kernel.Bind(x =>
                    x.From(assemblies)
                        .IncludingNonPublicTypes()
                        .SelectAllClasses()
                        .InheritedFrom<IRawTelemetryConnector>()
                        .BindAllInterfaces()
                        .Configure(c => c.InSingletonScope()));

                Logger.Info($"Connector loaded: {connectorDirectory.Name}");
            }

            var connectors = kernel.GetAll<IRawTelemetryConnector>().ToList();

            if (connectors.Count == 0)
            {
                MessageBox.Show($"No connectors loaded. Please place connectors .dll into {ConnectorsDir.FullName}", "No connectors", MessageBoxButton.OK);
                Environment.Exit(1);
            }
        }

        public static void LoadPlugins(this IKernel kernel)
        {
            var pluginDirectories = PluginsDir.EnumerateDirectories();

            foreach (var pluginDirectory in pluginDirectories)
            {
                Logger.Info($"Loading plugin: {pluginDirectory.Name}");
                var assemblies = LoadAssembliesFromDirectory(pluginDirectory);

                kernel.Load(assemblies);

                kernel.Bind(x =>
                    x.From(assemblies)
                        .SelectAllClasses()
                        .InheritedFrom<ISecondMonitorPlugin>()
                        .BindAllInterfaces()
                        .Configure(c => c.InSingletonScope()));

                Logger.Info($"Plugin loaded: {pluginDirectory.Name}");
            }

            var plugins = kernel.GetAll<ISecondMonitorPlugin>().ToList();

            if (plugins.Count == 0)
            {
                MessageBox.Show($"No plugins loaded. Please place plugins .dll into {PluginsDir.FullName}", "No plugins", MessageBoxButton.OK);
                Environment.Exit(1);
            }
        }

        private static IReadOnlyList<Assembly> LoadAssembliesFromDirectory(DirectoryInfo directoryInfo)
        {
            Logger.Info($"Loading assemblies from directory {directoryInfo.FullName} into current AppDomain");
            var collector = new AssemblyLoaderCollector();
            AppDomain.CurrentDomain.AssemblyLoad += collector.AssemblyLoaded;

            foreach (var assemblyFileInfo in directoryInfo.EnumerateFiles("*.dll", SearchOption.AllDirectories).Where(x => !AssembliesToIgnore.Contains(x.Name)))
            {
                try
                {
                    Assembly.LoadFrom(assemblyFileInfo.FullName);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            AppDomain.CurrentDomain.AssemblyLoad -= collector.AssemblyLoaded;

            Logger.Info($"{collector.LoadedAssemblies.Count} assemblies loaded into current AppDomain");

            return collector.LoadedAssemblies;
        }
    }
}
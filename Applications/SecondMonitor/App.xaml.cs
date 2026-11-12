// ReSharper disable once RedundantUsingDirective

namespace SecondMonitor
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows;
    using Bootstrapping;
    using Ninject;
    using NLog;
    using PluginManager.Core;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IKernel _kernel = StandardKernelFactory.CreateNew();

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                LogVersion();
                base.OnStartup(e);
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                _kernel.LoadCore();
                _kernel.LoadPlugins();
                _kernel.LoadConnectors();
                PluginsManager pluginsManager = _kernel.Get<PluginsManager>();
                await pluginsManager.InitializePlugins();
                await pluginsManager.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Application experienced an error");
                Environment.Exit(1);
            }
        }

        private static void LogVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Logger.Info($"Application Starting - Version: {version}");
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("Application experienced an unhandled excpetion");
            Logger.Error(e.ExceptionObject);
        }
    }
}
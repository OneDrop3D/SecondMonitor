namespace SecondMonitor.ManagePlugins
{
    using System;
    using System.Windows;
    using Ninject;
    using Ninject.Parameters;
    using NLog;
    using PluginsConfiguration.Application.Controllers;
    using PluginsConfiguration.Application.View;

    using SecondMonitor.Bootstrapping;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IKernel _kernel = StandardKernelFactory.CreateNew();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                _kernel.LoadCore();
                _kernel.LoadPlugins();
                _kernel.LoadConnectors();
                PluginsSettingsWindow pluginsSettingsWindow = new PluginsSettingsWindow();
                IPluginConfigurationController pluginConfigurationController = _kernel.Get<IPluginConfigurationController>(new ConstructorArgument("mainWindow", pluginsSettingsWindow));
                pluginsSettingsWindow.Closed += async (sender, args) =>
                {
                    await pluginConfigurationController.StopControllerAsync();
                    Current.Shutdown();
                };
                await pluginConfigurationController.StartControllerAsync();
                pluginsSettingsWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error Occured");
                Environment.Exit(1);
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Error("Application experienced an unhandled exception", e.ExceptionObject);
        }
    }
}
namespace SecondMonitor.TelemetryLauncher
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using Bootstrapping;
    using Ninject;
    using Ninject.Parameters;
    using NLog;
    using NLog.Config;
    using Telemetry.TelemetryApplication.Controllers;
    using TelemetryPresentation.MainWindow;

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
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LogManager.ThrowConfigExceptions = false;
                LogManager.Configuration = new XmlLoggingConfiguration(assemblyFolder + "\\NLogTelemetry.config");

                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

                _kernel.LoadCore();
                MainWindow mainWindow = new MainWindow();
                Logger.Info("-----------------------Application Started------------------------------------");
                TelemetryApplicationController telemetryApplicationController = _kernel.Get<TelemetryApplicationController>(new ConstructorArgument("mainWindow", mainWindow));
                await telemetryApplicationController.StartControllerAsync();
                mainWindow.Closed += async (sender, args) =>
                {
                    await telemetryApplicationController.StopControllerAsync();
                    Current.Shutdown();
                };
                await telemetryApplicationController.OpenLastSessionFromRepository();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error Occured");
                Environment.Exit(1);
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Error("Application experienced an unhandled excpetion");
            LogManager.GetCurrentClassLogger().Error(e.ExceptionObject);
        }
    }
}
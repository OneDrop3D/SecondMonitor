namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers
{
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;

    using MainWindow;
    using SecondMonitor.ViewModels.Controllers;

    public class TelemetryApplicationController : IController
    {
        //This is just a dummy to load TelemetryManagement for Kernel bootstrap
        private readonly Window _mainWindow;

        private readonly IMainWindowController _mainWindowController;

        public TelemetryApplicationController(Window mainWindow, IMainWindowController mainWindowController)
        {
            _mainWindow = mainWindow;
            _mainWindowController = mainWindowController;
        }

        public async Task StartControllerAsync()
        {
            await StartChildControllers();
        }

        public async Task StopControllerAsync()
        {
            await _mainWindowController.StopControllerAsync();
        }

        public async Task OpenFromRepository(string sessionIdentifier)
        {
            await _mainWindowController.LoadTelemetrySession(sessionIdentifier);
        }

        public async Task OpenLastSessionFromRepository()
        {
            await _mainWindowController.LoadLastTelemetrySession();
        }

        private async Task StartChildControllers()
        {
            Assembly.Load("TelemetryManagement");
            _mainWindowController.MainWindow = _mainWindow;
            await _mainWindowController.StartControllerAsync();
        }
    }
}
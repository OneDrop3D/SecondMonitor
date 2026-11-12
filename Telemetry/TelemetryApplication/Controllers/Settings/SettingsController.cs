namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings
{
    using System;
    using System.Threading.Tasks;
    using Car;
    using SecondMonitor.ViewModels.Controllers;
    using TelemetryApplication.Settings;
    using TelemetryApplication.Settings.DTO;
    using TelemetryManagement.Settings;
    using Workspaces.Providers.BuildIn;

    public class SettingsController : ISettingsController
    {
        private readonly ITelemetrySettingsRepository _telemetrySettingsRepository;

        public SettingsController(ITelemetrySettingsRepository telemetrySettingsRepository, IChildControllerFactory childControllerFactory)
        {
            _telemetrySettingsRepository = telemetrySettingsRepository;
            CarSettingsController = childControllerFactory.Create<ICarSettingsController, ISettingsController>(this);
        }

        public event EventHandler<SettingChangedArgs> SettingsChanged;

        public ICarSettingsController CarSettingsController { get; }
        public TelemetrySettingsDto TelemetrySettings { get; private set; }

        public async Task StartControllerAsync()
        {
            TelemetrySettings = _telemetrySettingsRepository.LoadOrCreateNew();
            if (TelemetrySettings.CarsProperties == null)
            {
                TelemetrySettings.CarsProperties = new CarsProperties();
            }

            if (TelemetrySettings.SelectedWorkspaceGuid == Guid.Empty)
            {
                TelemetrySettings.SelectedWorkspaceGuid = DefaultWorkspaceProvider.Guid;
            }

            await CarSettingsController.StartControllerAsync();
        }

        public async Task StopControllerAsync()
        {
            await CarSettingsController.StopControllerAsync();
            _telemetrySettingsRepository.SaveTelemetrySettings(TelemetrySettings);
        }

        public void SetTelemetrySettings(TelemetrySettingsDto telemetrySettings, RequestedAction action)
        {
            TelemetrySettings = telemetrySettings;
            SettingsChanged?.Invoke(this, new SettingChangedArgs(action));
        }
    }
}
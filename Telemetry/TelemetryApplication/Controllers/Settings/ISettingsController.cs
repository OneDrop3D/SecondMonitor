namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings
{
    using System;
    using Car;
    using SecondMonitor.ViewModels.Controllers;
    using TelemetryApplication.Settings.DTO;

    public interface ISettingsController : IController
    {
        event EventHandler<SettingChangedArgs> SettingsChanged;

        ICarSettingsController CarSettingsController { get; }

        TelemetrySettingsDto TelemetrySettings { get; }
        void SetTelemetrySettings(TelemetrySettingsDto telemetrySettings, RequestedAction action);
    }
}
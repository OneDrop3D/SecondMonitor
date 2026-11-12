namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;
    using TelemetryManagement.DTO;

    public class LapCarSettingsViewModel : AbstractViewModel<LapTelemetryDto>
    {
        private bool _isLoadCarSettingsEnabled;
        private ICommand _loadCarSettingsFromLapCommand;
        private ICommand _applyCarSettingsToLapCommand;
        private ICommand _clearCarSettingCommand;
        private string _name;

        public bool IsLoadCarSettingsEnabled
        {
            get => _isLoadCarSettingsEnabled;
            set => SetProperty(ref _isLoadCarSettingsEnabled, value);
        }

        public ICommand LoadCarSettingsFromLapCommand
        {
            get => _loadCarSettingsFromLapCommand;
            set => SetProperty(ref _loadCarSettingsFromLapCommand, value);
        }

        public ICommand ApplyCarSettingsToLapCommand
        {
            get => _applyCarSettingsToLapCommand;
            set => SetProperty(ref _applyCarSettingsToLapCommand, value);
        }

        public ICommand ClearCarSettingCommand
        {
            get => _clearCarSettingCommand;
            set => SetProperty(ref _clearCarSettingCommand, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        protected override void ApplyModel(LapTelemetryDto model)
        {
            IsLoadCarSettingsEnabled = model.CarPropertiesDto != null;
            Name = model.LapSummary.CustomDisplayName;
        }

        public override LapTelemetryDto SaveToNewModel()
        {
            throw new System.NotSupportedException();
        }
    }
}
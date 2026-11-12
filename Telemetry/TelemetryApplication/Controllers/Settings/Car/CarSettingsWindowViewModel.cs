namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using TelemetryManagement.DTO;

    public class CarSettingsWindowViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;

        public CarSettingsWindowViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            CarPropertiesViewModel = viewModelFactory.Create<CarPropertiesViewModel>();
            LapCarSettingsViewModels = new ObservableCollection<LapCarSettingsViewModel>();
        }

        public CarPropertiesViewModel CarPropertiesViewModel { get; }

        public ObservableCollection<LapCarSettingsViewModel> LapCarSettingsViewModels { get; }

        public void FillLaps(IEnumerable<LapTelemetryDto> laps)
        {
            LapCarSettingsViewModels.Clear();
            foreach (LapTelemetryDto lapTelemetryDto in laps)
            {
                var newViewModel = _viewModelFactory.Create<LapCarSettingsViewModel>();
                newViewModel.FromModel(lapTelemetryDto);
                LapCarSettingsViewModels.Add(newViewModel);
            }
        }
    }
}
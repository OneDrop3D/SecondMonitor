namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Contracts.Commands;
    using DefaultCarProperties;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using Synchronization;

    using TelemetryLoad;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;
    using TelemetryManagement.Settings.ChartProperties;
    using ViewModels;
    using ViewModels.LoadedLapCache;

    public class CarSettingsController : ICarSettingsController
    {
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly IWindowService _windowService;
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly ITelemetryLoadController _telemetryLoadController;
        private readonly List<IDefaultCarPropertiesProvider> _defaultCarPropertiesProviders;
        private readonly CarSettingsWindowViewModel _carSettingsWindowViewModel;
        private Window _settingsWindow;

        public CarSettingsController(ITelemetryViewsSynchronization telemetryViewsSynchronization, IEnumerable<IDefaultCarPropertiesProvider> defaultCarPropertiesProviders,
            IMainWindowViewModel mainWindowViewModel, IWindowService windowService, IViewModelFactory viewModelFactory, ILoadedLapsCache loadedLapsCache, ITelemetryLoadController telemetryLoadController)
        {
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _windowService = windowService;
            _loadedLapsCache = loadedLapsCache;
            _telemetryLoadController = telemetryLoadController;
            _defaultCarPropertiesProviders = defaultCarPropertiesProviders.ToList();
            mainWindowViewModel.LapSelectionViewModel.OpenCarSettingsCommand = new RelayCommand(OpenCarSettings);
            _carSettingsWindowViewModel = viewModelFactory.Create<CarSettingsWindowViewModel>();
            _carSettingsWindowViewModel.CarPropertiesViewModel.CancelCommand = new RelayCommand(CancelCarSettings);
            _carSettingsWindowViewModel.CarPropertiesViewModel.OkCommand = new RelayCommand(ConfirmCarSettings);
        }

        public event EventHandler<CarPropertiesArgs> CarPropertiesChanges;

        public ISettingsController ParentController { get; set; }

        public CarWithChartPropertiesDto CurrentCarWithChartProperties { get; private set; }

        public CarsProperties CarsProperties => ParentController.TelemetrySettings.CarsProperties;

        public Task StartControllerAsync()
        {
            _telemetryViewsSynchronization.NewSessionLoaded += TelemetryViewsSynchronizationOnNewSessionLoaded;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _telemetryViewsSynchronization.NewSessionLoaded -= TelemetryViewsSynchronizationOnNewSessionLoaded;
            return Task.CompletedTask;
        }

        public CarWithChartPropertiesDto ResetAndGetCarPropertiesForCurrentCar()
        {
            return ResetAndGetCarProperties(CurrentCarWithChartProperties.CarPropertiesDto.Simulator, CurrentCarWithChartProperties.CarPropertiesDto.CarName);
        }

        public CarWithChartPropertiesDto GetDefaultCarPropertiesForCurrentCar()
        {
            var carProperties = CreateDefaultCarProperties(CurrentCarWithChartProperties.CarPropertiesDto.Simulator, CurrentCarWithChartProperties.CarPropertiesDto.CarName);
            return carProperties;
        }

        public CarWithChartPropertiesDto GetCarProperties(string simulator, string carName)
        {
            if (CarsProperties.TryGetCarProperties(simulator, carName, out CarWithChartPropertiesDto carProperties))
            {
                if (carProperties.ChartsProperties.CamberHistogram == null)
                {
                    carProperties.ChartsProperties.CamberHistogram = new CamberHistogram();
                }

                return carProperties;
            }

            carProperties = CreateDefaultCarProperties(simulator, carName);
            CarsProperties.SaveCarProperties(carProperties);
            return carProperties;
        }

        public CarWithChartPropertiesDto ResetAndGetCarProperties(string simulator, string carName)
        {
            var carProperties = CreateDefaultCarProperties(simulator, carName);
            CarsProperties.SaveCarProperties(carProperties);
            return carProperties;
        }

        public void UpdateCarProperties(CarWithChartPropertiesDto carWithChartPropertiesDto)
        {
            CarsProperties.Cars.RemoveAll(x => x.CarPropertiesDto.Simulator == carWithChartPropertiesDto.CarPropertiesDto.Simulator && x.CarPropertiesDto.CarName == carWithChartPropertiesDto.CarPropertiesDto.CarName);
            CarsProperties.Cars.Add(carWithChartPropertiesDto);
            if (CurrentCarWithChartProperties.CarPropertiesDto.Simulator == carWithChartPropertiesDto.CarPropertiesDto.Simulator && CurrentCarWithChartProperties.CarPropertiesDto.CarName == carWithChartPropertiesDto.CarPropertiesDto.CarName)
            {
                CurrentCarWithChartProperties = carWithChartPropertiesDto;
            }

            NotifyCarPropertiesChanged(carWithChartPropertiesDto);
        }

        public CarPropertiesDto GetCarProperties(LapTelemetryDto lapTelemetryDto)
        {
            return lapTelemetryDto.CarPropertiesDto ?? CurrentCarWithChartProperties.CarPropertiesDto;
        }

        public CarPropertiesDto GetCarProperties(LapSummaryDto lapSummaryDto)
        {
            return _loadedLapsCache.TryGetLapTelemetry(lapSummaryDto, out LapTelemetryDto lapTelemetryDto) ? GetCarProperties(lapTelemetryDto) : CurrentCarWithChartProperties.CarPropertiesDto;
        }

        public IEnumerable<(LapTelemetryDto LapTelemetryDto, CarPropertiesDto CarPropertiesDto)> FillCarProperties(IEnumerable<LapTelemetryDto> lapTelemetries)
        {
            return lapTelemetries.Select(x => (x, GetCarProperties(x)));
        }

        private void NotifyCarPropertiesChanged(CarWithChartPropertiesDto carWithChartPropertiesDto)
        {
            CarPropertiesChanges?.Invoke(this, new CarPropertiesArgs(carWithChartPropertiesDto));
        }

        private CarWithChartPropertiesDto CreateDefaultCarProperties(string simulator, string carName)
        {
            CarWithChartPropertiesDto carWithChartPropertiesDto = new CarWithChartPropertiesDto();
            CarPropertiesDto carProperties = null;
            if (!_defaultCarPropertiesProviders.Any(x => x.TryGetDefaultSettings(simulator, carName, out carProperties)))
            {
                carProperties = new CarPropertiesDto()
                {
                    CarName = carName,
                    Simulator = simulator
                };
            }

            carWithChartPropertiesDto.CarPropertiesDto = carProperties;
            return carWithChartPropertiesDto;
        }

        private void TelemetryViewsSynchronizationOnNewSessionLoaded(object sender, TelemetrySessionArgs e)
        {
            CurrentCarWithChartProperties = GetCarProperties(e.SessionInfoDto.Simulator, e.SessionInfoDto.CarName);
        }

        private void OpenCarSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Focus();
                return;
            }

            var currentCarProperties = CurrentCarWithChartProperties;
            _carSettingsWindowViewModel.FillLaps(_loadedLapsCache.LoadedLaps);
            _carSettingsWindowViewModel.CarPropertiesViewModel.FromModel(currentCarProperties.CarPropertiesDto);
            foreach (LapCarSettingsViewModel lapCarSettingsViewModel in _carSettingsWindowViewModel.LapCarSettingsViewModels)
            {
                lapCarSettingsViewModel.ApplyCarSettingsToLapCommand = new RelayCommand(() => ApplyCarSettings(lapCarSettingsViewModel));
                lapCarSettingsViewModel.LoadCarSettingsFromLapCommand = new RelayCommand(() => LoadCarSettingCommand(lapCarSettingsViewModel));
                lapCarSettingsViewModel.ClearCarSettingCommand = new RelayCommand(() => ClearLapSettingsCommand(lapCarSettingsViewModel));
            }

            _settingsWindow = _windowService.OpenWindow(_carSettingsWindowViewModel, "Car Properties", WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterOwner, _ => OnSettingsWindowClose());
        }

        private void ClearLapSettingsCommand(LapCarSettingsViewModel viewModel)
        {
            viewModel.OriginalModel.CarPropertiesDto = null;
            viewModel.IsLoadCarSettingsEnabled = false;
            _telemetryLoadController.MarkLapAsModified(viewModel.OriginalModel.LapSummary);
        }

        private void LoadCarSettingCommand(LapCarSettingsViewModel viewModel)
        {
            _carSettingsWindowViewModel.CarPropertiesViewModel.FromModel(viewModel.OriginalModel.CarPropertiesDto);
        }

        private void ApplyCarSettings(LapCarSettingsViewModel viewModel)
        {
            viewModel.OriginalModel.CarPropertiesDto = _carSettingsWindowViewModel.CarPropertiesViewModel.SaveToNewModel();
            _telemetryLoadController.MarkLapAsModified(viewModel.OriginalModel.LapSummary);
            viewModel.IsLoadCarSettingsEnabled = true;
        }

        private void OnSettingsWindowClose()
        {
            _settingsWindow = null;
        }

        private void CancelCarSettings()
        {
            _settingsWindow?.Close();
            //Update with old data to trigger notify (for cases when changes in lap settings happened)
            UpdateCarProperties(CurrentCarWithChartProperties);
        }

        private void ConfirmCarSettings()
        {
            var newCarSettings = _carSettingsWindowViewModel.CarPropertiesViewModel.SaveToNewModel();
            CurrentCarWithChartProperties.CarPropertiesDto = newCarSettings;
            UpdateCarProperties(CurrentCarWithChartProperties);
            _settingsWindow?.Close();
        }
    }
}
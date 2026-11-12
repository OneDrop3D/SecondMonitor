namespace SecondMonitor.Timing.Application.Controllers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;
    using Contracts.Commands;
    using SecondMonitor.Telemetry.TelemetryApplication.Checks;
    using SecondMonitor.Telemetry.TelemetryApplication.Repository;
    using SecondMonitor.Telemetry.TelemetryManagement.Repository;
    using ViewModel.TelemetryChecklist;
    using ViewModels;
    using ViewModels.Factory;
    using ViewModels.Settings;

    public class OpenTelemetryChecklistController : IOpenTelemetryChecklistController
    {
        private readonly ITelemetryRepository _telemetryRepository;
        private readonly ITelemetryCheckController _telemetryCheckController;
        private readonly IWindowService _windowService;
        private readonly IViewModelFactory _viewModelFactory;
        private Window _lapPickerWindow;

        public OpenTelemetryChecklistController(ITelemetryRepositoryFactory telemetryRepository, ITelemetryCheckController telemetryCheckController,
            ISettingsProvider settingsProvider, IWindowService windowService, IViewModelFactory viewModelFactory)
        {
            _telemetryRepository = telemetryRepository.Create(settingsProvider);
            _telemetryCheckController = telemetryCheckController;
            _windowService = windowService;
            _viewModelFactory = viewModelFactory;
        }

        public Task StartControllerAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }

        public void OpenLapSelection(DriverTiming sessionTimingPlayer)
        {
            _lapPickerWindow?.Close();
            if (sessionTimingPlayer.CompletedLaps == 0)
            {
                return;
            }

            List<ILapInfo> eligibleLaps = sessionTimingPlayer.Laps.Where(x => !string.IsNullOrEmpty(x.LapTelemetryInfo?.TelemetryFilePath)).Reverse().ToList();

            if (eligibleLaps.Count == 0)
            {
                return;
            }

            LapListViewModel lapListViewModel = _viewModelFactory.Create<LapListViewModel>();
            lapListViewModel.ApplyLaps(sessionTimingPlayer.BestLap, eligibleLaps.First(), eligibleLaps);

            lapListViewModel.BestLap.OpenCommand = new RelayCommand(() => OpenTelemetryChecklist(lapListViewModel.BestLap.OriginalModel));
            lapListViewModel.LastLap.OpenCommand = new RelayCommand(() => OpenTelemetryChecklist(lapListViewModel.LastLap.OriginalModel));
            lapListViewModel.AllLaps.ForEach(x => x.OpenCommand = new RelayCommand(() => OpenTelemetryChecklist(x.OriginalModel)));

            _lapPickerWindow = _windowService.OpenWindow(lapListViewModel, "Select Lap", WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterOwner, Application.Current.MainWindow);
            _lapPickerWindow.MaxHeight = 600;
        }

        private void OpenTelemetryChecklist(ILapInfo lapInfo)
        {
            _lapPickerWindow.Close();
            if (string.IsNullOrEmpty(lapInfo.LapTelemetryInfo.TelemetryFilePath))
            {
                return;
            }

            FileInfo testTelemetry = new FileInfo(lapInfo.LapTelemetryInfo.TelemetryFilePath);
            var lapTelemetry = _telemetryRepository.LoadLapTelemetryDto(testTelemetry);
            _telemetryCheckController.OpenCheckWindow(lapTelemetry);
        }
    }
}
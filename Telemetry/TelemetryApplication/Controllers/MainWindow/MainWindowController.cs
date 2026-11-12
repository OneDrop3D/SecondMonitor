namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow
{
    using System.Threading.Tasks;
    using System.Windows;
    using AggregatedChart;
    using Contracts.Commands;
    using LapPicker;
    using MapView;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using Settings;
    using Snapshot;
    using Synchronization;
    using Synchronization.Graphs;
    using TelemetryLoad;
    using ViewModels;
    using Workspace;

    public class MainWindowController : IMainWindowController
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ITelemetryLoadController _telemetryLoadController;
        private readonly ILapPickerController _lapPickerController;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IMainWindowViewModel _mainWindowViewModel;
        private readonly ISnapshotSectionController _snapshotSectionController;
        private readonly IMapViewController _mapViewController;
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly IAggregatedChartsController _aggregatedChartsController;
        private readonly ISettingsController _settingsController;
        private readonly IWorkspaceController _workspaceController;

        public MainWindowController(ISettingsProvider settingsProvider, ITelemetryLoadController telemetryLoadController, ILapPickerController lapPickerController, IViewModelFactory viewModelFactory, IMainWindowViewModel mainWindowViewModel,
            ISnapshotSectionController snapshotSectionController, IMapViewController mapViewController, ITelemetryViewsSynchronization telemetryViewsSynchronization, IAggregatedChartsController aggregatedChartsController, ISettingsController settingsController, IWorkspaceController workspaceController)
        {
            _settingsProvider = settingsProvider;
            _telemetryLoadController = telemetryLoadController;
            _lapPickerController = lapPickerController;
            _viewModelFactory = viewModelFactory;
            _mainWindowViewModel = mainWindowViewModel;
            _snapshotSectionController = snapshotSectionController;
            _mapViewController = mapViewController;
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _aggregatedChartsController = aggregatedChartsController;
            _settingsController = settingsController;
            _workspaceController = workspaceController;

            _snapshotSectionController.MainWindowViewModel = _mainWindowViewModel;
            _mapViewController.MapViewViewModel = _mainWindowViewModel.MapViewViewModel;
        }

        public Window MainWindow { get; set; }

        public async Task LoadTelemetrySession(string telemetryIdentifier)
        {
            await _telemetryLoadController.LoadRecentSessionAsync(telemetryIdentifier);
        }

        public async Task LoadLastTelemetrySession()
        {
            await _telemetryLoadController.LoadLastSessionAsync();
        }

        public async Task StartControllerAsync()
        {
            MainWindow.DataContext = _mainWindowViewModel;
            ShowMainWindow();
            await StartChildControllers();
        }

        public async Task StopControllerAsync()
        {
            await StopChildControllers();
        }

        private async Task StartChildControllers()
        {
            Subscribe();
            _mainWindowViewModel.CollapseLapPickerCommand = new RelayCommand(SwitchLapSelectionPickerCollapse);
            await _settingsController.StartControllerAsync();
            await _telemetryLoadController.StartControllerAsync();
            await _workspaceController.StartControllerAsync();
            await _lapPickerController.StartControllerAsync();
            await _snapshotSectionController.StartControllerAsync();
            await _mapViewController.StartControllerAsync();
            await _aggregatedChartsController.StartControllerAsync();
        }

        private async Task StopChildControllers()
        {
            await _telemetryLoadController.StopControllerAsync();
            await _workspaceController.StopControllerAsync();
            await _lapPickerController.StopControllerAsync();
            await _snapshotSectionController.StopControllerAsync();
            await _mapViewController.StopControllerAsync();
            await _aggregatedChartsController.StopControllerAsync();
            await _settingsController.StopControllerAsync();
            UnSubscribe();
        }

        private void ShowMainWindow()
        {
            MainWindow.Show();
        }

        private void Subscribe()
        {
            _telemetryViewsSynchronization.BusyStateChanged += TelemetryViewsSynchronizationOnBusyStateChanged;
        }

        private void UnSubscribe()
        {
            _telemetryViewsSynchronization.BusyStateChanged -= TelemetryViewsSynchronizationOnBusyStateChanged;
        }

        private void TelemetryViewsSynchronizationOnBusyStateChanged(object sender, BusyStateEventArgs e)
        {
            _mainWindowViewModel.IsBusy = e.IsBusy;
        }

        private void SwitchLapSelectionPickerCollapse()
        {
            _mainWindowViewModel.IsLapPickerVisible = !_mainWindowViewModel.IsLapPickerVisible;
        }
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.SettingsWindow
{
    using System.Threading.Tasks;
    using System.Windows;
    using Contracts.Commands;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;
    using Settings;
    using TelemetryApplication.Settings.DTO;
    using ViewModels;
    using ViewModels.SettingsWindow;

    public class SettingsWindowController : ISettingsWindowController
    {
        private readonly ISettingsController _settingsController;
        private readonly IWindowService _windowService;
        private readonly TelemetrySettingsViewModel _telemetrySettingsViewModel;
        private readonly WorkspaceEditorController _workspaceEditorController;
        private Window _settingsWindow;

        public SettingsWindowController(IMainWindowViewModel mainWindowViewModel, IViewModelFactory viewModelFactory, ISettingsController settingsController, IWindowService windowService, IChildControllerFactory childControllerFactory)
        {
            _settingsController = settingsController;
            _windowService = windowService;
            _telemetrySettingsViewModel = viewModelFactory.Create<TelemetrySettingsViewModel>();
            mainWindowViewModel.LapSelectionViewModel.OpenSettingsWindowCommand = new RelayCommand(OpenSettingsWindow);
            _workspaceEditorController = childControllerFactory.Create<WorkspaceEditorController, ISettingsWindowController>(this);
            BindCommands();
        }

        public async Task StartControllerAsync()
        {
            await _workspaceEditorController.StartControllerAsync();
        }

        public async Task StopControllerAsync()
        {
            await _workspaceEditorController.StartControllerAsync();
        }

        private void OpenSettingsWindow()
        {
            if (_settingsWindow?.IsVisible == true)
            {
                _settingsWindow.Focus();
                return;
            }

            TelemetrySettingsDto telemetrySettingsDto = _settingsController.TelemetrySettings;
            _telemetrySettingsViewModel.FromModel(telemetrySettingsDto);
            _workspaceEditorController.UpdateWorkspaceViewmodel(_telemetrySettingsViewModel.WorkspacesSettingsViewModel, telemetrySettingsDto);
            _settingsWindow = _windowService.OpenWindow(_telemetrySettingsViewModel, "Telemetry Settings", WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterScreen);
        }

        private void SaveAndClose()
        {
            TelemetrySettingsDto newTelemetrySettingsDto = ((TelemetrySettingsViewModel)_settingsWindow.Content).SaveToNewModel();
            _settingsController.SetTelemetrySettings(newTelemetrySettingsDto, RequestedAction.RefreshCharts);
            CloseWindow();
        }

        private void CloseWindow()
        {
            _settingsWindow.DataContext = null;
            _settingsWindow.Close();
            _settingsWindow = null;
        }

        private void BindCommands()
        {
            _telemetrySettingsViewModel.CancelCommand = new RelayCommand(CloseWindow);
            _telemetrySettingsViewModel.OkCommand = new RelayCommand(SaveAndClose);
        }
    }
}
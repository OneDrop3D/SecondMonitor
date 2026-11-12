namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.LapsCharts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows;

    using SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.LapPicker;
    using SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapsChart;
    using SecondMonitor.Telemetry.TelemetryManagement.DTO;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;

    public class LapsChartsController : AbstractChildController<ILapPickerController>, ILapsChartsController
    {
        private readonly IWindowService _windowService;
        private Window _openedWindow;
        private LapsChartWindowViewModel _windowViewModel;

        public LapsChartsController(IWindowService windowService, IViewModelFactory viewModelFactory) : base(viewModelFactory)
        {
            _windowService = windowService;
        }

        public void OpenOrFocusLapChartsWindow(IReadOnlyList<LapSummaryDto> allAvailableLaps, IReadOnlyList<string> selectedLapsIds)
        {
            if (_openedWindow != null)
            {
                _openedWindow.Focus();
                return;
            }

            _windowViewModel = ViewModelFactory.Create<LapsChartWindowViewModel>();
            _windowViewModel.SetLapSummaries(allAvailableLaps, selectedLapsIds);

            _openedWindow = _windowService.OpenWindow(_windowViewModel, "Laps Chart", WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterOwner);
            _openedWindow.Closed += OpenedWindowOnClosed;
        }

        private void OpenedWindowOnClosed(object sender, EventArgs e)
        {
            _openedWindow.Closed -= OpenedWindowOnClosed;
            _openedWindow = null;
            _windowViewModel = null;
        }

        public override Task StartControllerAsync()
        {
            return Task.CompletedTask;
        }

        public override Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }
    }
}

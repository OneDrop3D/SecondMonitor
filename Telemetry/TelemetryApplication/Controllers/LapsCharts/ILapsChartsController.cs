namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.LapsCharts
{
    using System.Collections.Generic;

    using SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.LapPicker;
    using SecondMonitor.Telemetry.TelemetryManagement.DTO;
    using SecondMonitor.ViewModels.Controllers;

    public interface ILapsChartsController : IChildController<ILapPickerController>
    {
        void OpenOrFocusLapChartsWindow(IReadOnlyList<LapSummaryDto> allAvailableLaps, IReadOnlyList<string> selectedLapsIds);
    }
}

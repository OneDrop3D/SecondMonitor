namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;

    public interface IWorkspacesSeriesChartsController : IController
    {
        IViewModel GetChartByName(string name);
        void DisposeAllPreviouslyCreatedCharts();
    }
}
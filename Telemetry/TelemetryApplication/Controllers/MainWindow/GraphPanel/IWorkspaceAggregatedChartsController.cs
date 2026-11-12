namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;

    public interface IWorkspaceAggregatedChartsController : IController
    {
        IViewModel GetChartByName(string name);
    }
}
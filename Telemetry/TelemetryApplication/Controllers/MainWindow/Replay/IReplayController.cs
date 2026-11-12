namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.Replay
{
    using SecondMonitor.ViewModels.Controllers;
    using ViewModels.SnapshotSection;

    public interface IReplayController : IController
    {
        ISnapshotSectionViewModel SnapshotSectionViewModel { set; }
    }
}
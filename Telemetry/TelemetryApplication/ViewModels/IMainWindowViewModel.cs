namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels
{
    using System.Windows.Input;
    using LapPicker;
    using MapView;
    using SecondMonitor.ViewModels;
    using SnapshotSection;
    using Workspace;

    public interface IMainWindowViewModel : IViewModel
    {
        bool IsBusy { get; set; }
        LapSelectionViewModel LapSelectionViewModel { get; }
        ISnapshotSectionViewModel SnapshotSectionViewModel { get; }
        IMapViewViewModel MapViewViewModel { get; }

        WorkspaceViewModel WorkspaceViewModel { get; }

        bool IsLapPickerVisible { get; set; }

        ICommand CollapseLapPickerCommand { get; set; }
    }
}
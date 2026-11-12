namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels
{
    using System.Windows.Input;
    using LapPicker;
    using MapView;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SnapshotSection;
    using Workspace;

    public class MainWindowViewModel : AbstractViewModel, IMainWindowViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isBusy;
        private bool _isLapPickerVisible;
        private ICommand _collapseLapPickerCommand;

        public MainWindowViewModel(IViewModelFactory viewModelFactory)
        {
            IsLapPickerVisible = true;
            _viewModelFactory = viewModelFactory;
            LapSelectionViewModel = viewModelFactory.Create<LapSelectionViewModel>();
            SnapshotSectionViewModel = viewModelFactory.Create<ISnapshotSectionViewModel>();
            MapViewViewModel = viewModelFactory.Create<IMapViewViewModel>();
            WorkspaceViewModel = viewModelFactory.Create<WorkspaceViewModel>();
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public LapSelectionViewModel LapSelectionViewModel { get; }
        public ISnapshotSectionViewModel SnapshotSectionViewModel { get; }
        public IMapViewViewModel MapViewViewModel { get; }

        public WorkspaceViewModel WorkspaceViewModel { get; set; }

        public bool IsLapPickerVisible
        {
            get => _isLapPickerVisible;
            set => SetProperty(ref _isLapPickerVisible, value);
        }

        public ICommand CollapseLapPickerCommand
        {
            get => _collapseLapPickerCommand;
            set => SetProperty(ref _collapseLapPickerCommand, value);
        }
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.SettingsWindow
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using Settings.DTO;
    using Workspace;

    public class TelemetrySettingsViewModel : AbstractViewModel<TelemetrySettingsDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private ICommand _cancelCommand;
        private ICommand _okCommand;
        private XAxisKind _xAxisKind;

        public TelemetrySettingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            WorkspacesSettingsViewModel = viewModelFactory.Create<WorkspacesSettingsViewModel>();
            AggregatedChartSettingsViewModel = viewModelFactory.Create<AggregatedChartSettingsViewModel>();
        }

        public XAxisKind XAxisKind
        {
            get => _xAxisKind;
            set => SetProperty(ref _xAxisKind, value);
        }

        public WorkspacesSettingsViewModel WorkspacesSettingsViewModel { get; }

        public AggregatedChartSettingsViewModel AggregatedChartSettingsViewModel { get; }

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => SetProperty(ref _cancelCommand, value);
        }

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public override TelemetrySettingsDto SaveToNewModel()
        {
            var newModel = new TelemetrySettingsDto()
            {
                XAxisKind = XAxisKind,
                AggregatedChartSettings = AggregatedChartSettingsViewModel.SaveToNewModel(),
                CarsProperties = OriginalModel.CarsProperties,
                SelectedWorkspaceGuid = WorkspacesSettingsViewModel.SelectedWorkspaceSetting.WorkspaceId,
                CustomWorkspaces = WorkspacesSettingsViewModel.GetNonBuildInWorkspaces(),
                CustomChartSets = WorkspacesSettingsViewModel.GetNonBuildInChartSet(),
            };
            return newModel;
        }

        protected override void ApplyModel(TelemetrySettingsDto model)
        {
            XAxisKind = model.XAxisKind;
            AggregatedChartSettingsViewModel.FromModel(model.AggregatedChartSettings);
        }
    }
}
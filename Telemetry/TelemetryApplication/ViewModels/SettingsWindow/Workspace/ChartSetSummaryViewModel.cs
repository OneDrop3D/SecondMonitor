namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.SettingsWindow.Workspace
{
    using System;
    using System.Windows.Input;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Layouts;
    using Settings.Workspace;

    public class ChartSetSummaryViewModel : AbstractViewModel<ChartSet>
    {
        private string _name;
        private Guid _guid;
        private bool _isBuildIn;
        private bool _isDeleteButtonVisible;
        private ICommand _deleteCommand;
        private LayoutDescription _layoutDescription;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public Guid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }

        public bool IsBuildIn
        {
            get => _isBuildIn;
            set => SetProperty(ref _isBuildIn, value);
        }

        public bool IsDeleteButtonVisible
        {
            get => _isDeleteButtonVisible;
            set => SetProperty(ref _isDeleteButtonVisible, value);
        }

        public ICommand DeleteCommand
        {
            get => _deleteCommand;
            set => SetProperty(ref _deleteCommand, value);
        }

        public LayoutDescription LayoutDescription
        {
            get => _layoutDescription;
            set => SetProperty(ref _layoutDescription, value);
        }

        protected override void ApplyModel(ChartSet model)
        {
            Name = model.Name;
            IsBuildIn = model.IsBuildIn;
            Guid = model.Guid;
            LayoutDescription = model.LayoutDescription;
        }

        public override ChartSet SaveToNewModel()
        {
            return new ChartSet()
            {
                Favourite = OriginalModel.Favourite,
                Guid = Guid,
                Name = Name,
                LayoutDescription = LayoutDescription,
            };
        }
    }
}
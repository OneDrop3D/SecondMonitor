namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model;

    public class PitEstimationVisualizationSettingsViewModel : AbstractViewModel<PitEstimationVisualizationSettings>
    {
        private bool _isPitBoardEnabled;
        private bool _isMapEnabled;

        public bool IsPitBoardEnabled
        {
            get => _isPitBoardEnabled;
            set => SetProperty(ref _isPitBoardEnabled, value);
        }

        public bool IsMapEnabled
        {
            get => _isMapEnabled;
            set => SetProperty(ref _isMapEnabled, value);
        }

        protected override void ApplyModel(PitEstimationVisualizationSettings model)
        {
            IsPitBoardEnabled = model.IsPitBoardEnabled;
            IsMapEnabled = model.IsMapEnabled;
        }

        public override PitEstimationVisualizationSettings SaveToNewModel()
        {
            return new PitEstimationVisualizationSettings(IsPitBoardEnabled, IsMapEnabled);
        }
    }
}
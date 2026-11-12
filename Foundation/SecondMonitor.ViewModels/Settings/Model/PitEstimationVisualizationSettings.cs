namespace SecondMonitor.ViewModels.Settings.Model
{
    public class PitEstimationVisualizationSettings
    {
        public PitEstimationVisualizationSettings(bool isPitBoardEnabled, bool isMapEnabled)
        {
            IsPitBoardEnabled = isPitBoardEnabled;
            IsMapEnabled = isMapEnabled;
        }

        public PitEstimationVisualizationSettings()
        {
        }

        public bool IsPitBoardEnabled { get; set; }

        public bool IsMapEnabled { get; set; }
    }
}
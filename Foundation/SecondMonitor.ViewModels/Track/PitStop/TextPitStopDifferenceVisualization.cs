namespace SecondMonitor.ViewModels.Track.PitStop
{
    public class TextPitStopDifferenceVisualization : AbstractViewModel
    {
        private string _information;

        public TextPitStopDifferenceVisualization()
        {
            Information = string.Empty;
        }

        public string Information
        {
            get => _information;
            set => SetProperty(ref _information, value);
        }
    }
}
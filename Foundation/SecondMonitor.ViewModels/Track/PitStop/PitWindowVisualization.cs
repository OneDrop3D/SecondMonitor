namespace SecondMonitor.ViewModels.Track.PitStop
{
    public class PitWindowVisualization : AbstractViewModel
    {
        private bool _isPitStopCompleted;

        public bool IsPitStopCompleted
        {
            get => _isPitStopCompleted;
            set => SetProperty(ref _isPitStopCompleted, value);
        }
    }
}
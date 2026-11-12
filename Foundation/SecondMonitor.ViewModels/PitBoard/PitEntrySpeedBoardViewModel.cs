namespace SecondMonitor.ViewModels.PitBoard
{
    public class PitEntrySpeedBoardViewModel : AbstractViewModel
    {
        private string _speedFormatted;

        public PitEntrySpeedBoardViewModel()
        {
            SpeedFormatted = "50.0 Kph";
        }

        public string SpeedFormatted
        {
            get => _speedFormatted;
            set => SetProperty(ref _speedFormatted, value);
        }
    }
}
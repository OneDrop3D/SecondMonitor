namespace SecondMonitor.SimdataManagement.RaceSuggestion.ViewModels
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public class RaceSuggestionViewModel : AbstractViewModel
    {
        public RaceSuggestionViewModel(string simulatorName, string trackName, string carClassName, string carName, int difficulty)
        {
            SimulatorName = simulatorName;
            TrackName = trackName;
            CarClassName = carClassName;
            CarName = carName;
            Difficulty = difficulty;
            IsDifficultyVisible = difficulty > 0;
        }

        public string SimulatorName { get; }
        public string TrackName { get; }
        public string CarClassName { get; }
        public string CarName { get; }
        public int Difficulty { get; }
        public bool IsDifficultyVisible { get; }
        public ICommand SuggestSimulatorCommand { get; set; }
        public ICommand SuggestClassCommand { get; set; }
        public ICommand SuggestCarCommand { get; set; }
        public ICommand SuggestTrackCommand { get; set; }
    }
}
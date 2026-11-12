namespace SecondMonitor.SimdataManagement.RaceSuggestion.ViewModels
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public class SuggestionsViewModel : AbstractViewModel
    {
        private bool _isVisible;
        private ICommand _closeCommand;

        public SuggestionsViewModel()
        {
            RacesSuggestionsViewModel = new RacesSuggestionsViewModel();
            TrackSuggestionViewModel = new TrackSuggestionViewModel();
        }

        public TrackSuggestionViewModel TrackSuggestionViewModel { get; }
        public RacesSuggestionsViewModel RacesSuggestionsViewModel { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => SetProperty(ref _closeCommand, value);
        }
    }
}
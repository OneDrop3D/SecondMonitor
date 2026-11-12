namespace SecondMonitor.SimdataManagement.RaceSuggestion.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public class RacesSuggestionsViewModel : AbstractViewModel
    {
        private ICommand _randomizeCommand;

        public RacesSuggestionsViewModel()
        {
            RaceSuggestions = new ObservableCollection<RaceSuggestionViewModel>();
        }

        public ICommand RandomizeCommand
        {
            get => _randomizeCommand;
            set => SetProperty(ref _randomizeCommand, value);
        }

        public ICollection<RaceSuggestionViewModel> RaceSuggestions { get; }

        public void AddRaceSuggestion(RaceSuggestionViewModel raceSuggestionViewModel)
        {
            RaceSuggestions.Add(raceSuggestionViewModel);
        }

        public void ClearRaceSuggestions()
        {
            RaceSuggestions.Clear();
        }
    }
}
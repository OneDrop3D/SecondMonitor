namespace SecondMonitor.SimdataManagement.RaceSuggestion
{
    using SecondMonitor.ViewModels.Controllers;
    using ViewModels;

    public interface IRaceSuggestionController : IController
    {
        SuggestionsViewModel SuggestionsViewModel { get; }

        void ToggleRaceSuggestionViewVisibility();
    }
}
namespace SecondMonitor.SimdataManagement.RaceSuggestion.ViewModels
{
    using System.Collections.Generic;
    using System.Windows.Input;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Track;

    public class TrackSuggestionViewModel : AbstractViewModel
    {
        private List<string> _availableSims;
        private string _selectedSim;
        private string _trackName;

        public TrackSuggestionViewModel()
        {
            _availableSims = new List<string>();
            TrackGeometryViewModel = new TrackGeometryViewModel();
        }

        public List<string> AvailableSims
        {
            get => _availableSims;
            set => SetProperty(ref _availableSims, value);
        }

        public string SelectedSim
        {
            get => _selectedSim;
            set
            {
                SetProperty(ref _selectedSim, value);
                RandomizeCommand?.Execute(null);
            }
        }

        public string TrackName
        {
            get => _trackName;
            set => SetProperty(ref _trackName, value);
        }

        public ICommand RandomizeCommand { get; set; }

        public TrackGeometryViewModel TrackGeometryViewModel { get; }
    }
}
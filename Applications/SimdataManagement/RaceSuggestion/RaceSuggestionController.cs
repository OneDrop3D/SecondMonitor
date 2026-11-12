namespace SecondMonitor.SimdataManagement.RaceSuggestion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Contracts.Commands;
    using Contracts.Rating;
    using Contracts.TrackMap;
    using DataModel;
    using DataModel.SimulatorContent;
    using DataModel.TrackMap;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.SimulatorContent;
    using ViewModels;

    public class RaceSuggestionController : IRaceSuggestionController
    {
        private static readonly string[] SupportedSimulator = { SimulatorsNameMap.AcSimName, SimulatorsNameMap.PCars2SimName,
            SimulatorsNameMap.R3ESimName, SimulatorsNameMap.AccSimName, SimulatorsNameMap.Ams2SimName,
            SimulatorsNameMap.AmsSimName, SimulatorsNameMap.Rf2SimName, SimulatorsNameMap.Gtr2SimName, SimulatorsNameMap.LmUSimName };

        private readonly int _numberOfSuggestions;
        private readonly ISimulatorContentProvider _simulatorContentProvider;
        private readonly ISimulatorRatingProvider _simulatorRatingProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly Random _random;
        private readonly IMapsLoader _mapsLoader;

        public RaceSuggestionController(IViewModelFactory viewModelFactory, ISimulatorContentProvider simulatorContentProvider, ISimulatorRatingProvider simulatorRatingProvider, ISessionEventProvider sessionEventProvider, IMapsLoaderFactory mapsLoaderFactory)
        {
            _numberOfSuggestions = 15;
            _random = new Random();
            _simulatorContentProvider = simulatorContentProvider;
            _simulatorRatingProvider = simulatorRatingProvider;
            _sessionEventProvider = sessionEventProvider;
            SuggestionsViewModel = viewModelFactory.Create<SuggestionsViewModel>();
            SuggestionsViewModel.CloseCommand = new RelayCommand(CloseViewModel);
            SuggestionsViewModel.RacesSuggestionsViewModel.RandomizeCommand = new RelayCommand(CreateNewRaceSuggestions);
            SuggestionsViewModel.TrackSuggestionViewModel.RandomizeCommand = new RelayCommand(CreateNewTrackSuggestion);
            SuggestionsViewModel.IsVisible = false;
            _mapsLoader = mapsLoaderFactory.Create();
        }

        public SuggestionsViewModel SuggestionsViewModel { get; }
        public void ToggleRaceSuggestionViewVisibility()
        {
            SuggestionsViewModel.IsVisible = !SuggestionsViewModel.IsVisible;
            if (SuggestionsViewModel.IsVisible)
            {
                RefreshTrackSuggestionViewModel();
                CreateNewRaceSuggestions();
            }
        }

        public Task StartControllerAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }

        private void CloseViewModel()
        {
            SuggestionsViewModel.IsVisible = false;
        }

        private void CreateNewRaceSuggestions()
        {
            SuggestionsViewModel.RacesSuggestionsViewModel.ClearRaceSuggestions();
            var simulators = _simulatorContentProvider.GetSimulatorsWithContent().Where(x => SupportedSimulator.Contains(x)).ToList();
            string currentSim = _sessionEventProvider.CurrentSimName;
            IEnumerable<RaceSuggestionViewModel> suggestions;
            if (string.IsNullOrEmpty(currentSim) || SimulatorsNameMap.IsNotConnected(currentSim) || !simulators.Contains(currentSim))
            {
                suggestions = Enumerable.Range(0, _numberOfSuggestions).Select(_ => CreateRandomSuggestion(simulators));
            }
            else
            {
                suggestions = Enumerable.Range(0, _numberOfSuggestions).Select(_ => CreateRandomSuggestion(currentSim, string.Empty, string.Empty, string.Empty));
            }

            suggestions.ForEach(SuggestionsViewModel.RacesSuggestionsViewModel.AddRaceSuggestion);
        }

        private void CreateNewRaceSuggestions(string simulatorName)
        {
            SuggestionsViewModel.RacesSuggestionsViewModel.ClearRaceSuggestions();
            var suggestions = Enumerable.Range(0, _numberOfSuggestions).Select(_ => CreateRandomSuggestion(simulatorName, string.Empty, string.Empty, string.Empty));
            suggestions.ForEach(SuggestionsViewModel.RacesSuggestionsViewModel.AddRaceSuggestion);
        }

        private void CreateNewRaceSuggestions(string simulatorName, string carClassName, string carName, string trackName)
        {
            SuggestionsViewModel.RacesSuggestionsViewModel.ClearRaceSuggestions();
            var suggestions = Enumerable.Range(0, _numberOfSuggestions).Select(_ => CreateRandomSuggestion(simulatorName, carClassName, carName, trackName));
            suggestions.ForEach(SuggestionsViewModel.RacesSuggestionsViewModel.AddRaceSuggestion);
        }

        private RaceSuggestionViewModel CreateRandomSuggestion(List<string> simulators)
        {
            string selectedSimulator = simulators[_random.Next(simulators.Count)];
            return CreateRandomSuggestion(selectedSimulator, string.Empty, string.Empty, string.Empty);
        }

        private RaceSuggestionViewModel CreateRandomSuggestion(string simulator, string carClassName, string carName, string trackName)
        {
            var carClasses = _simulatorContentProvider.GetAllCarClassesForSimulator(simulator).Concat(new[] { new CarClass("Your Pick") }).ToList();
            if (string.IsNullOrEmpty(trackName))
            {
                var tracks = _simulatorContentProvider.GetAllTracksForSimulator(simulator).Select(x => x.Name).Concat(new[] { "Your Pick" }).ToList();
                trackName = tracks[_random.Next(tracks.Count)];
            }

            var carClass = string.IsNullOrEmpty(carClassName) ? carClasses[_random.Next(carClasses.Count)] : carClasses.First(x => x.ClassName == carClassName);

            if (string.IsNullOrEmpty(carName))
            {
                var carsOfSelectedClass = carClass.Cars.Select(x => x.CarName).ToList();
                carName = carsOfSelectedClass.Count > 0 ? carsOfSelectedClass[_random.Next(carsOfSelectedClass.Count)] : "Your Pick";
            }

            return CreateNewSuggestion(simulator, carClass.ClassName, carName, trackName);
        }

        private RaceSuggestionViewModel CreateNewSuggestion(string simulator, string carClass, string carName, string trackName)
        {
            _simulatorRatingProvider.TryGetSuggestedDifficulty(simulator, carClass, out int difficulty);
            var raceSuggestion = new RaceSuggestionViewModel(simulator, trackName, carClass, carName, difficulty)
            {
                SuggestSimulatorCommand = new RelayCommand(() => CreateNewRaceSuggestions(simulator)),
                SuggestCarCommand = new RelayCommand(() => CreateNewRaceSuggestions(simulator, carClass, carName, string.Empty)),
                SuggestClassCommand = new RelayCommand(() => CreateNewRaceSuggestions(simulator, carClass, string.Empty, string.Empty)),
                SuggestTrackCommand = new RelayCommand(() => CreateNewRaceSuggestions(simulator, string.Empty, string.Empty, trackName)),
            };
            return raceSuggestion;
        }

        private void RefreshTrackSuggestionViewModel()
        {
            var availableSims = _simulatorContentProvider.GetSimulatorsWithContent().OrderBy(x => x).ToList();
            SuggestionsViewModel.TrackSuggestionViewModel.AvailableSims = availableSims;
            if (availableSims.Count == 0)
            {
                return;
            }

            string currentSim = _sessionEventProvider.CurrentSimName;
            if (string.IsNullOrEmpty(currentSim) || SimulatorsNameMap.IsNotConnected(currentSim) || !availableSims.Contains(currentSim))
            {
                SuggestionsViewModel.TrackSuggestionViewModel.SelectedSim = availableSims[0];
            }
            else
            {
                SuggestionsViewModel.TrackSuggestionViewModel.SelectedSim = currentSim;
            }
        }

        private void CreateNewTrackSuggestion()
        {
            string selectedSim = SuggestionsViewModel.TrackSuggestionViewModel.SelectedSim;
            if (string.IsNullOrEmpty(selectedSim))
            {
                return;
            }

            var selectedTrack = _simulatorContentProvider.GetRandomTrack(selectedSim);

            if (selectedTrack == null)
            {
                return;
            }

            _mapsLoader.TryLoadMap(selectedSim, selectedTrack.Name, out TrackMapDto trackMapDto);
            SuggestionsViewModel.TrackSuggestionViewModel.TrackName = selectedTrack.Name;
            SuggestionsViewModel.TrackSuggestionViewModel.TrackGeometryViewModel.FromModel(trackMapDto?.TrackGeometry);
        }
    }
}
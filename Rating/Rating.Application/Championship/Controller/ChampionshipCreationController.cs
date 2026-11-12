namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Common.Championship.Calendar.Templates.CalendarGroups;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Calendar;
    using Contracts.Commands;
    using Contracts.TrackMap;

    using DataModel.SimulatorContent;
    using DataModel.TrackMap;

    using Rating.Controller.RaceObserver;

    using SecondMonitor.DataModel;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.SimulatorContent;
    using ViewModels.Creation;
    using ViewModels.Creation.Calendar;
    using ViewModels.Creation.Calendar.Predefined;

    public class ChampionshipCreationController : AbstractChildController<IChampionshipOverviewController>, IChampionshipCreationController
    {
        private static readonly string[] teamScoringSupportedSim = [SimulatorsNameMap.R3ESimName, SimulatorsNameMap.AccSimName, SimulatorsNameMap.LmUSimName, SimulatorsNameMap.F12024];
        private readonly IWindowService _windowService;
        private readonly ISimulatorContentProvider _simulatorContentProvider;
        private readonly ITrackTemplateToSimTrackMapper _trackTemplateToSimTrackMapper;
        private readonly IChampionshipFactory _championshipFactory;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IStoredCalendarsAccessor _storedCalendarsAccessor;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IMapsLoader _mapsLoader;
        private Window _dialogWindow;
        private Action<ChampionshipDto> _newChampionshipCallback;
        private Action _cancellationCallback;
        private bool _championshipCreated;
        private ChampionshipCreationViewModel _championshipCreationViewModel;
        private string _selectedSimulator;

        public ChampionshipCreationController(IWindowService windowService, IViewModelFactory viewModelFactory, ISimulatorContentProvider simulatorContentProvider, IMapsLoaderFactory mapsLoaderFactory, ITrackTemplateToSimTrackMapper trackTemplateToSimTrackMapper,
            IChampionshipFactory championshipFactory, ISessionEventProvider sessionEventProvider, IStoredCalendarsAccessor storedCalendarsAccessor, ISettingsProvider settingsProvider) : base(viewModelFactory)
        {
            _windowService = windowService;
            _simulatorContentProvider = simulatorContentProvider;
            _trackTemplateToSimTrackMapper = trackTemplateToSimTrackMapper;
            _championshipFactory = championshipFactory;
            _sessionEventProvider = sessionEventProvider;
            _storedCalendarsAccessor = storedCalendarsAccessor;
            _settingsProvider = settingsProvider;
            _mapsLoader = mapsLoaderFactory.Create();
        }

        public override Task StartControllerAsync()
        {
            Bind<ChampionshipCreationViewModel>().Command(x => x.ConfirmSimulatorCommand).To(ConfirmSimulatorSelection);
            Bind<ChampionshipCreationViewModel>().Command(x => x.OkCommand).To(CreateNewChampionship);
            Bind<ChampionshipCreationViewModel>().Command(x => x.CancelCommand).To(CancelChampionshipCreation);

            Bind<CreatedCalendarViewModel>().Command(x => x.RandomCalendarCommand).To(CreateRandomCalendar);
            Bind<CreatedCalendarViewModel>().Command(x => x.SelectPredefinedCalendarCommand).To(SelectPredefinedCalendar);
            Bind<CreatedCalendarViewModel>().Command(x => x.LoadCalendarCommand).To(LoadCalendar);
            Bind<CreatedCalendarViewModel>().Command(x => x.SaveCalendarCommand).To(SaveCalendar);
            return Task.CompletedTask;
        }

        public override Task StopControllerAsync()
        {
            _dialogWindow?.Close();
            _trackTemplateToSimTrackMapper.SaveTrackMappings();
            return Task.CompletedTask;
        }

        public void TryFocusCreationWindow()
        {
            _dialogWindow?.Focus();
        }

        public void OpenChampionshipCreationDialog(Action<ChampionshipDto> newChampionshipCallback, Action cancellationCallback)
        {
            _newChampionshipCallback = newChampionshipCallback;
            _cancellationCallback = cancellationCallback;
            _championshipCreationViewModel = ViewModelFactory.Create<ChampionshipCreationViewModel>();
            _championshipCreationViewModel.IsSimulatorSelectionEnabled = true;
            _championshipCreationViewModel.AvailableSimulators = RaceObserverController.SupportedSimulators.OrderBy(x => x).ToArray();

            if (_sessionEventProvider.IsConnectedToSim() && RaceObserverController.SupportedSimulators.Contains(_sessionEventProvider.CurrentSimName))
            {
                _championshipCreationViewModel.SelectedSimulator = _sessionEventProvider.CurrentSimName;
            }

            _championshipCreationViewModel.ApplySettings(_settingsProvider.DisplaySettingsViewModel.ChampionshipSettingViewModel.LastChampionshipOptions);

            _dialogWindow = _windowService.OpenWindow(_championshipCreationViewModel, "New Championship", WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterOwner, _ => DialogWindowClosed());
        }

        private void CancelChampionshipCreation()
        {
            _dialogWindow.Close();
        }

        private void CreateNewChampionship()
        {
            _championshipCreated = true;
            ChampionshipDto newChampionshipDto = _championshipFactory.Create(_championshipCreationViewModel);
            _settingsProvider.DisplaySettingsViewModel.ChampionshipSettingViewModel.LastChampionshipOptions = _championshipCreationViewModel.GetChampionshipOptions();
            _newChampionshipCallback(newChampionshipDto);
            //_dialogWindow.Close();
        }

        private void CreateRandomCalendar()
        {
            Random random = new Random();
            int randomEvents = _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.RandomEventsCount;
            List<ExistingTrackTemplateViewModel> tracksToChooseFrom = _championshipCreationViewModel.CalendarDefinitionViewModel.AvailableTracksViewModel.GetAllExistingTracks();
            if (tracksToChooseFrom.Count == 0)
            {
                return;
            }

            _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.ClearCalendar();

            for (int i = 0; i < randomEvents; i++)
            {
                int newTrackIndex = random.Next(tracksToChooseFrom.Count - 1);
                _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.AppendNewEntry(tracksToChooseFrom[newTrackIndex]);
                tracksToChooseFrom.RemoveAt(newTrackIndex);
                if (tracksToChooseFrom.Count == 0)
                {
                    tracksToChooseFrom = _championshipCreationViewModel.CalendarDefinitionViewModel.AvailableTracksViewModel.GetAllExistingTracks();
                }
            }
        }

        private void SelectPredefinedCalendar()
        {
            var calendarSelection = ViewModelFactory.Create<PredefinedCalendarSelectionViewModel>();
            calendarSelection.FromModel(AllGroups.MainGroup);
            _windowService.OpenDialog(calendarSelection, "Select Predefined Calendar", WindowState.Normal, SizeToContent.Manual, WindowStartupLocation.CenterOwner);
            if (calendarSelection.DialogResult == true && calendarSelection.SelectedItem is CalendarTemplateViewModel selectedViewModel)
            {
                if (calendarSelection.UseEventNames)
                {
                    _championshipCreationViewModel.ChampionshipTitle = selectedViewModel.Title;
                }

                _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.CalendarYear = selectedViewModel.CalendarYear;

                _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.ApplyCalendarTemplate(selectedViewModel.OriginalModel, calendarSelection.UseEventNames, calendarSelection.AutoReplaceTracks);
            }
        }

        private void SaveCalendar()
        {
            CalendarDto calendarDto = new CalendarDto()
            {
                Name = _championshipCreationViewModel.ChampionshipTitle,
                Simulator = _selectedSimulator,
                Events = _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.ToCalendarEventsDto().ToList(),
            };

            if (_championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.IsCalendarDatesEnabled)
            {
                calendarDto.IsChampionshipDatesFilled = true;
                calendarDto.StartDate = _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.StartOfCalendar;
                calendarDto.EndDate = _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.EndOfCalendar;
            }

            _storedCalendarsAccessor.TrySaveCalendar(calendarDto);
        }

        private void LoadCalendar()
        {
            if (_storedCalendarsAccessor.TryLoadCalendar(out CalendarDto calendarDto))
            {
                Apply(calendarDto);
            }
        }

        private void Apply(CalendarDto calendarDto)
        {
            _championshipCreationViewModel.ChampionshipTitle = calendarDto.Name;
            _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.ApplyCalendarDto(calendarDto);
        }

        private void ConfirmSimulatorSelection()
        {
            if (string.IsNullOrWhiteSpace(_championshipCreationViewModel.SelectedSimulator))
            {
                return;
            }

            _championshipCreationViewModel.IsSimulatorSelectionEnabled = false;
            _championshipCreationViewModel.IsTeamScoringEnabled = teamScoringSupportedSim.Contains(_championshipCreationViewModel.SelectedSimulator);
            _championshipCreationViewModel.IsTeamScoringCheckBoxVisible = _championshipCreationViewModel.IsTeamScoringEnabled;
            _selectedSimulator = _championshipCreationViewModel.SelectedSimulator;

            _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.SimulatorName = _selectedSimulator;
            _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.IsCalendarDatesEnabled = true; //SimsWithDateSupport.Contains(_selectedSimulator);

            var allTracks = _simulatorContentProvider.GetAllTracksForSimulator(_selectedSimulator).OrderBy(x => x.Name);
            List<AbstractTrackTemplateViewModel> tracksTemplates = new List<AbstractTrackTemplateViewModel> { ViewModelFactory.Create<GenericTrackTemplateViewModel>(), ViewModelFactory.Create<MysteryTrackTemplateViewModel>() };
            tracksTemplates.ForEach(x => x.UseTemplateInCalendarCommand = new RelayCommand(UseTemplateInCalendar));
            foreach (Track currentTrack in allTracks)
            {
                var newViewModel = ViewModelFactory.Create<ExistingTrackTemplateViewModel>();
                newViewModel.TrackName = currentTrack.Name;
                newViewModel.LayoutLengthMeters = currentTrack.LapDistance;
                newViewModel.UseTemplateInCalendarCommand = new RelayCommand(UseTemplateInCalendar);
                bool hasMap = _mapsLoader.TryLoadMap(_selectedSimulator, currentTrack.Name, out TrackMapDto trackMapDto);
                if (hasMap)
                {
                    newViewModel.TrackGeometryViewModel.FromModel(trackMapDto.TrackGeometry);
                }

                tracksTemplates.Add(newViewModel);
            }

            _championshipCreationViewModel.CalendarDefinitionViewModel.AvailableTracksViewModel.FromModel(tracksTemplates);
            _championshipCreationViewModel.ClassesSelectionViewModel.FromModel(_simulatorContentProvider.GetAllCarClassesForSimulator(_selectedSimulator).Where(x => x.ClassName != "Unknown").Select(x => x.ClassName));
        }

        private void UseTemplateInCalendar()
        {
            _championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.AppendNewEntry(_championshipCreationViewModel.CalendarDefinitionViewModel.AvailableTracksViewModel.SelectedTrackTemplateViewModel);
        }

        private void DialogWindowClosed()
        {
            _dialogWindow = null;
            if (!_championshipCreated)
            {
                _cancellationCallback();
            }
        }
    }
}
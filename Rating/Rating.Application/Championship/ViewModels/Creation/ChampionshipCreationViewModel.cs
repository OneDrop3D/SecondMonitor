namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Championship;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings.Model;

    using Session;

    public class ChampionshipCreationViewModel : AbstractViewModel
    {
        private bool _isSimulatorSelectionEnabled;
        private string _championshipTitle;
        private bool _aiNamesCanChange;
        private bool _isClassChangeEnabled;
        private bool _isMysteryClass;

        private bool _isOkButtonEnabled;
        private ICommand _okCommand;
        private ICommand _cancelCommand;
        private bool _isManufacturerScoringEnabled;
        private bool _isTeamScoringEnabled;
        private bool _isTeamScoringCheckBoxVisible;
        private HumanReadableItem<ScoringKind> _selectedTeamsScoring;
        private HumanReadableItem<ScoringKind> _selectedManufacturerScoring;
        private bool _isTrackAllClassesEnabled;

        public ChampionshipCreationViewModel(IViewModelFactory viewModelFactory)
        {
            CalendarDefinitionViewModel = viewModelFactory.Create<CalendarDefinitionViewModel>();
            SessionsDefinitionViewModel = viewModelFactory.Create<SessionsDefinitionViewModel>();
            ClassesSelectionViewModel = viewModelFactory.Create<ClassesSelectionViewModel>();
            IsClassChangeEnabled = true;
            AiNamesCanChange = true;
            IsManufacturerScoringEnabled = true;
            IsTeamScoringEnabled = true;
            IsTeamScoringCheckBoxVisible = true;
            ChampionshipTitle = "Custom Championship";
            CalendarDefinitionViewModel.CalendarViewModel.PropertyChanged += CalendarViewModelOnPropertyChanged;
            CalendarDefinitionViewModel.CalendarViewModel.CalendarEntries.CollectionChanged += CalendarEntriesOnCollectionChanged;
            AllAvailableScoringKinds = new ScoringKindMap().GetAllItems().ToList();
            SelectedManufacturerScoring = AllAvailableScoringKinds.First();
            SelectedTeamsScoring = AllAvailableScoringKinds.First();
        }

        public bool IsSimulatorSelectionEnabled
        {
            get => _isSimulatorSelectionEnabled;
            set => SetProperty(ref _isSimulatorSelectionEnabled, value);
        }

        public CalendarDefinitionViewModel CalendarDefinitionViewModel { get; }

        public SessionsDefinitionViewModel SessionsDefinitionViewModel { get; }

        public ClassesSelectionViewModel ClassesSelectionViewModel { get;  }

        public string[] AvailableSimulators { get; set; }

        public string SelectedSimulator { get; set; }

        public string ChampionshipTitle
        {
            get => _championshipTitle;
            set => SetProperty(ref _championshipTitle, value);
        }

        public ICommand ConfirmSimulatorCommand { get; set; }

        public bool AiNamesCanChange
        {
            get => _aiNamesCanChange;
            set => SetProperty(ref _aiNamesCanChange, value);
        }

        public bool IsManufacturerScoringEnabled
        {
            get => _isManufacturerScoringEnabled;
            set => SetProperty(ref _isManufacturerScoringEnabled, value);
        }

        public bool IsTeamScoringEnabled
        {
            get => _isTeamScoringEnabled;
            set => SetProperty(ref _isTeamScoringEnabled, value);
        }
        
        public bool IsTeamScoringCheckBoxVisible
        {
            get => _isTeamScoringCheckBoxVisible;
            set => SetProperty(ref _isTeamScoringCheckBoxVisible, value);
        }

        public bool IsOkButtonEnabled
        {
            get => _isOkButtonEnabled;
            set => SetProperty(ref _isOkButtonEnabled, value);
        }

        public List<HumanReadableItem<ScoringKind>> AllAvailableScoringKinds { get; }

        public HumanReadableItem<ScoringKind> SelectedTeamsScoring
        {
            get => _selectedTeamsScoring;
            set => SetProperty(ref _selectedTeamsScoring, value);
        }

        public HumanReadableItem<ScoringKind> SelectedManufacturerScoring
        {
            get => _selectedManufacturerScoring;
            set => SetProperty(ref _selectedManufacturerScoring, value);
        }

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => SetProperty(ref _cancelCommand, value);
        }

        public bool IsClassChangeEnabled
        {
            get => _isClassChangeEnabled;
            set => SetProperty(ref _isClassChangeEnabled, value);
        }

        public bool IsMysteryClass
        {
            get => _isMysteryClass;
            set => SetProperty(ref _isMysteryClass, value);
        }

        public bool IsTrackAllClassesEnabled
        {
            get => _isTrackAllClassesEnabled;
            set => SetProperty(ref _isTrackAllClassesEnabled, value);
        }

        public void ApplySettings(ChampionshipOptions championshipOptions)
        {
            CalendarDefinitionViewModel.CalendarViewModel.StartOfCalendar = championshipOptions.StartOfCalendar;
            CalendarDefinitionViewModel.CalendarViewModel.EndOfCalendar = championshipOptions.EndOfCalendar;
            SelectedManufacturerScoring = AllAvailableScoringKinds.Single(x => x.Value == championshipOptions.ManufacturingScoring);
            SelectedTeamsScoring = AllAvailableScoringKinds.Single(x => x.Value == championshipOptions.TeamScoring);
            CalendarDefinitionViewModel.CalendarViewModel.SelectedRaceDay = CalendarDefinitionViewModel.CalendarViewModel.AllAvailableRaceDay.Single(x => x.Value == championshipOptions.RaceDay);
            IsTrackAllClassesEnabled = championshipOptions.IsTrackAllClassesEnabled;
        }

        public ChampionshipOptions GetChampionshipOptions()
        {
            return new ChampionshipOptions()
            {
                ManufacturingScoring = SelectedManufacturerScoring.Value,
                TeamScoring = SelectedTeamsScoring.Value,
                RaceDay = CalendarDefinitionViewModel.CalendarViewModel.SelectedRaceDay.Value,
                StartOfCalendar = CalendarDefinitionViewModel.CalendarViewModel.StartOfCalendar.Date,
                EndOfCalendar = CalendarDefinitionViewModel.CalendarViewModel.EndOfCalendar.Date,
                IsTrackAllClassesEnabled = IsTrackAllClassesEnabled,
            };
        }

        private void CalendarEntriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ReevaluateIsOkButtonEnabled();
            UpdateTrackUsagesCount(e.NewItems ?? new List<ExistingTrackCalendarEntryViewModel>(), e.OldItems ?? new List<ExistingTrackCalendarEntryViewModel>());
        }

        private void UpdateTrackUsagesCount(IList addedItems, IList removedItems)
        {
            foreach (ExistingTrackCalendarEntryViewModel addedTrack in addedItems.OfType<ExistingTrackCalendarEntryViewModel>())
            {
                ExistingTrackTemplateViewModel trackTemplate = CalendarDefinitionViewModel.AvailableTracksViewModel.GetTrackByName(addedTrack.TrackName);
                if (trackTemplate != null)
                {
                    trackTemplate.UsageCount++;
                }
            }

            foreach (ExistingTrackCalendarEntryViewModel removedTrack in removedItems.OfType<ExistingTrackCalendarEntryViewModel>())
            {
                ExistingTrackTemplateViewModel trackTemplate = CalendarDefinitionViewModel.AvailableTracksViewModel.GetTrackByName(removedTrack.TrackName);
                if (trackTemplate != null)
                {
                    trackTemplate.UsageCount--;
                }
            }
        }

        private void CalendarViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReevaluateIsOkButtonEnabled();
        }

        private void ReevaluateIsOkButtonEnabled()
        {
            IsOkButtonEnabled = CalendarDefinitionViewModel.CalendarViewModel.CalendarEntries.Count > 0 && CalendarDefinitionViewModel.CalendarViewModel.StartOfCalendar < CalendarDefinitionViewModel.CalendarViewModel.EndOfCalendar;
        }
    }
}
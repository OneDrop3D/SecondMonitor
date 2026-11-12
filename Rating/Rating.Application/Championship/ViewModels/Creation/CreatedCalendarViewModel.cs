namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Calendar;
    using Common.Championship.Calendar;
    using Common.DataModel.Championship.Calendar;
    using Contracts.Commands;
    using Controller;

    using GongSolutions.Wpf.DragDrop;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Championship;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Adorners;

    public class CreatedCalendarViewModel : AbstractViewModel, IDropTarget
    {
        private readonly ICalendarEntryViewModelFactory _calendarEntryViewModelFactory;
        private readonly ITrackTemplateToSimTrackMapper _trackTemplateToSimTrackMapper;
        private readonly IDialogService _dialogService;
        private int _totalEvents;
        private int _randomEventsCount;
        private int _calendarYear;
        private ICommand _randomCalendarCommand;
        private ICommand _loadCalendarCommand;
        private ICommand _saveCalendarCommand;
        private DateTime _startOfCalendar;
        private DateTime _endOfCalendar;
        private bool _isCalendarDatesEnabled;
        private ObservableCollection<AbstractCalendarEntryViewModel> _calendarEntries;
        private ICommand _shuffleCalendarCommand;
        private HumanReadableItem<RaceDay> _selectedRaceDay;

        public CreatedCalendarViewModel(ICalendarEntryViewModelFactory calendarEntryViewModelFactory, ITrackTemplateToSimTrackMapper trackTemplateToSimTrackMapper, IDialogService dialogService)
        {
            _calendarEntryViewModelFactory = calendarEntryViewModelFactory;
            _trackTemplateToSimTrackMapper = trackTemplateToSimTrackMapper;
            _dialogService = dialogService;
            RandomEventsCount = 6;

            _startOfCalendar = new DateTime(DateTime.Now.Year, 3, 1);
            _endOfCalendar = new DateTime(DateTime.Now.Year, 11, 30);
            _calendarYear = DateTime.Now.Year;

            CalendarEntries = new ObservableCollection<AbstractCalendarEntryViewModel>();
            ShuffleCalendarCommand = new RelayCommand(ShuffleCalendar);
            AllAvailableRaceDay = new ObservableCollection<HumanReadableItem<RaceDay>>(new RaceDayMap().GetAllItems());
            SelectedRaceDay = AllAvailableRaceDay.First(x => x.Value == RaceDay.Sunday);
        }

        public ObservableCollection<HumanReadableItem<RaceDay>> AllAvailableRaceDay { get; }

        public HumanReadableItem<RaceDay> SelectedRaceDay
        {
            get => _selectedRaceDay;
            set => SetProperty(ref _selectedRaceDay, value, (_, __) => RecalculateEventDates());
        }

        public ObservableCollection<AbstractCalendarEntryViewModel> CalendarEntries
        {
            get => _calendarEntries;
            set => SetProperty(ref _calendarEntries, value);
        }

        public string SimulatorName { get; set; }

        public int TotalEvents
        {
            get => _totalEvents;
            set => SetProperty(ref _totalEvents, value);
        }

        public bool IsCalendarDatesEnabled
        {
            get => _isCalendarDatesEnabled;
            set => SetProperty(ref _isCalendarDatesEnabled, value);
        }

        public ICommand SelectPredefinedCalendarCommand
        {
            get;
            set;
        }

        public ICommand LoadCalendarCommand
        {
            get => _loadCalendarCommand;
            set => SetProperty(ref _loadCalendarCommand, value);
        }

        public ICommand ShuffleCalendarCommand
        {
            get => _shuffleCalendarCommand;
            set => SetProperty(ref _shuffleCalendarCommand, value);
        }

        public ICommand SaveCalendarCommand
        {
            get => _saveCalendarCommand;
            set => SetProperty(ref _saveCalendarCommand, value);
        }

        public int CalendarYear
        {
            get => _calendarYear;
            set
            {
                SetProperty(ref _calendarYear, value);
                int yearDifference = _endOfCalendar.Year - _startOfCalendar.Year;
                _startOfCalendar = new DateTime(value, StartOfCalendar.Month, StartOfCalendar.Day);
                _endOfCalendar = new DateTime(value + yearDifference, EndOfCalendar.Month, EndOfCalendar.Day);
                NotifyPropertyChanged(nameof(StartOfCalendar));
                NotifyPropertyChanged(nameof(EndOfCalendar));
                OnCalendarDatesChanges();
            }
        }

        public DateTime StartOfCalendar
        {
            get => _startOfCalendar;
            set
            {
                SetProperty(ref _startOfCalendar, value);
                _calendarYear = value.Year;
                NotifyPropertyChanged(nameof(CalendarYear));
                RecalculateEventDates();
            }
        }

        public DateTime EndOfCalendar
        {
            get => _endOfCalendar;
            set
            {
                SetProperty(ref _endOfCalendar, value);
                RecalculateEventDates();
            }
        }

        public int RandomEventsCount
        {
            get => _randomEventsCount;
            set => SetProperty(ref _randomEventsCount, value);
        }

        public ICommand RandomCalendarCommand
        {
            get => _randomCalendarCommand;
            set => SetProperty(ref _randomCalendarCommand, value);
        }

        public void ApplyCalendarTemplate(CalendarTemplate calendarTemplate, bool useCalendarEventNames, bool autoReplaceKnownTracks)
        {
            CalendarEntries.Clear();
            foreach (EventTemplate calendarTemplateEvent in calendarTemplate.Events)
            {
                var newEntry = _calendarEntryViewModelFactory.Create(calendarTemplateEvent, SimulatorName, useCalendarEventNames, autoReplaceKnownTracks);
                newEntry.DeleteEntryCommand = new RelayCommand(() => DeleteCalendarEntry(newEntry));
                newEntry.ShowDateTimePicker = IsCalendarDatesEnabled;
                CalendarEntries.Add(newEntry);
            }

            RecalculateEventNumbers();
            if (calendarTemplate.Events.Any(x => !x.EventDate.HasValue))
            {
                RecalculateEventDates();
            }
            else
            {
                _startOfCalendar = calendarTemplate.Events[0].EventDate ?? DateTime.Now;
                _endOfCalendar = calendarTemplate.Events[calendarTemplate.Events.Count - 1].EventDate ?? DateTime.Now;
                NotifyPropertyChanged(nameof(StartOfCalendar));
                NotifyPropertyChanged(nameof(EndOfCalendar));
            }

            UpdateCalendarBoundariesOnEntries();
        }

        public void ApplyCalendarDto(CalendarDto calendarDto)
        {
            CalendarEntries.Clear();
            foreach (AbstractCalendarEntryViewModel newEntry in _calendarEntryViewModelFactory.Create(calendarDto, SimulatorName))
            {
                newEntry.ShowDateTimePicker = _isCalendarDatesEnabled;
                newEntry.DeleteEntryCommand = new RelayCommand(() => DeleteCalendarEntry(newEntry));
                newEntry.CalendarStart = StartOfCalendar;
                newEntry.CalendarEnd = EndOfCalendar;
                CalendarEntries.Add(newEntry);
            }

            if (calendarDto.IsChampionshipDatesFilled)
            {
                _startOfCalendar = calendarDto.StartDate;
                _endOfCalendar = calendarDto.EndDate;
                _calendarYear = _startOfCalendar.Year;
                NotifyPropertyChanged(nameof(CalendarYear));
                NotifyPropertyChanged(nameof(StartOfCalendar));
                NotifyPropertyChanged(nameof(EndOfCalendar));
            }

            if (calendarDto.IsChampionshipDatesFilled && calendarDto.Events.Any(x => !x.EventDate.HasValue))
            {
                RecalculateEventDates();
            }

            RecalculateEventNumbers();
        }

        public void DragEnter(IDropInfo dropInfo)
        {
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var target = (dropInfo.VisualTarget as FrameworkElement)?.DataContext;
            switch (target)
            {
                case CreatedCalendarViewModel _:
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.NotHandled = false;
                    return;
                case CalendarPlaceholderEntryViewModel _ when dropInfo.Data is ExistingTrackTemplateViewModel:
                    dropInfo.DropTargetAdorner = typeof(AllowDropAdorner);
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.NotHandled = false;
                    return;
            }

            dropInfo.DropTargetAdorner = typeof(ForbidDropAdorner);
            dropInfo.Effects = DragDropEffects.None;
            dropInfo.NotHandled = false;
        }

        public void DragLeave(IDropInfo dropInfo)
        {
        }

        public void Drop(IDropInfo dropInfo)
        {
            object target = (dropInfo.VisualTarget as FrameworkElement)?.DataContext;
            if (dropInfo.Data is AbstractCalendarEntryViewModel abstractCalendarEntry && ReferenceEquals(target, this))
            {
                MoveCalendarEntry(abstractCalendarEntry, dropInfo.InsertIndex);
                return;
            }

            if (target is CalendarPlaceholderEntryViewModel calendarPlaceholder && dropInfo.Data is ExistingTrackTemplateViewModel existingTrackTemplateViewModel)
            {
                ReplacePlaceHolder(calendarPlaceholder, existingTrackTemplateViewModel);
                return;
            }

            if (dropInfo.Data is AbstractTrackTemplateViewModel trackTemplate && ReferenceEquals(target, this))
            {
                CreateEntry(trackTemplate, dropInfo.InsertIndex);
            }
        }

        public void ClearCalendar()
        {
            CalendarEntries.Clear();
        }

        public void AppendNewEntry(AbstractTrackTemplateViewModel trackTemplate)
        {
            CreateEntry(trackTemplate, CalendarEntries.Count);
        }

        public IEnumerable<CalendarEventDto> ToCalendarEventsDto()
        {
            return CalendarEntries.Select(x => new CalendarEventDto()
            {
                EventName = x.CustomEventName,
                TrackName = x.TrackName,
                IsMysteryTrack = x is MysteryCalendarEntryViewModel,
                IsTrackNameExact = x is ExistingTrackCalendarEntryViewModel,
                EventDate = x.DateTime,
            });
        }

        private void ReplacePlaceHolder(CalendarPlaceholderEntryViewModel calendarPlaceholder, ExistingTrackTemplateViewModel existingTrackTemplateViewModel)
        {
            int index = _calendarEntries.IndexOf(calendarPlaceholder);
            _calendarEntries.Remove(calendarPlaceholder);
            CreateEntry(existingTrackTemplateViewModel, index, calendarPlaceholder.CustomEventName);
            if (_dialogService.ShowYesNoDialog("Always Replace", $"Do you want to always replace \n'{calendarPlaceholder.TrackName}' with \n'{existingTrackTemplateViewModel.TrackName}'?"))
            {
                _trackTemplateToSimTrackMapper.RegisterSimulatorTrackName(SimulatorName, calendarPlaceholder.TrackName, existingTrackTemplateViewModel.TrackName);
            }
        }

        private void DeleteCalendarEntry(AbstractCalendarEntryViewModel entryToDelete)
        {
            CalendarEntries.Remove(entryToDelete);
            RecalculateEventNumbers();
            RecalculateEventDates();
        }

        private void RecalculateEventNumbers()
        {
            TotalEvents = CalendarEntries.Count;
            for (int i = 0; i < CalendarEntries.Count; i++)
            {
                CalendarEntries[i].EventNumber = i + 1;
                CalendarEntries[i].OriginalEventName = "Round " + (i + 1);
            }
        }

        private void CreateEntry(AbstractTrackTemplateViewModel trackTemplate, int insertionIndex, string customEventName = "")
        {
            var newEntry = _calendarEntryViewModelFactory.Create(trackTemplate);
            newEntry.DeleteEntryCommand = new RelayCommand(() => DeleteCalendarEntry(newEntry));
            newEntry.CustomEventName = customEventName;
            CalendarEntries.Insert(insertionIndex, newEntry);
            RecalculateEventNumbers();
            RecalculateEventDates();
        }

        private void MoveCalendarEntry(AbstractCalendarEntryViewModel entry, int insertPosition)
        {
            int oldIndex = CalendarEntries.IndexOf(entry);
            if (oldIndex < insertPosition)
            {
                CalendarEntries.Move(oldIndex, Math.Max(0, insertPosition - 1));
            }
            else
            {
                CalendarEntries.Move(oldIndex, Math.Min(CalendarEntries.Count - 1, insertPosition));
            }

            RecalculateEventNumbers();
            RecalculateEventDates();
        }

        private void OnCalendarDatesChanges()
        {
            RecalculateEventDates();
        }

        private void UpdateCalendarBoundariesOnEntries()
        {
            foreach (AbstractCalendarEntryViewModel entry in CalendarEntries)
            {
                entry.CalendarStart = StartOfCalendar;
                entry.CalendarEnd = EndOfCalendar;
            }
        }

        private void RecalculateEventDates()
        {
            if (!IsCalendarDatesEnabled)
            {
                CalendarEntries.ForEach(x =>
                {
                    x.ShowDateTimePicker = IsCalendarDatesEnabled;
                    x.DateTime = null;
                });

                return;
            }

            if (StartOfCalendar >= EndOfCalendar)
            {
                return;
            }

            DateTime firstEventDateTime = StartOfCalendar;
            DateTime lastEventDateTime = EndOfCalendar - TimeSpan.FromDays(7);

            double totalDays = (lastEventDateTime - firstEventDateTime).TotalDays;

            if (totalDays < 7 || CalendarEntries.Count <= 1)
            {
                foreach (var calendarEntryViewModel in CalendarEntries)
                {
                    calendarEntryViewModel.ShowDateTimePicker = true;
                    calendarEntryViewModel.CalendarStart = StartOfCalendar;
                    calendarEntryViewModel.CalendarEnd = EndOfCalendar;
                    calendarEntryViewModel.DateTime = StartOfCalendar;
                }

                return;
            }

            double daysBetweenEvents = totalDays / (CalendarEntries.Count - 1);

            for (int i = 0; i < CalendarEntries.Count; i++)
            {
                var calendarEntryViewModel = CalendarEntries[i];
                DateTime currentEventDate = GetNextRaceDay(firstEventDateTime.AddDays(daysBetweenEvents * i).Date);
                calendarEntryViewModel.ShowDateTimePicker = true;
                calendarEntryViewModel.DateTime = currentEventDate;
            }

            UpdateCalendarBoundariesOnEntries();
        }

        private void ShuffleCalendar()
        {
            List<AbstractCalendarEntryViewModel> allEntries = CalendarEntries.ToList();
            bool hasValidDates = allEntries.All(x => x.DateTime != null);
            Queue<DateTime> eventDates = new Queue<DateTime>();
            if (hasValidDates)
            {
                allEntries
                    .Where(x => x.DateTime.HasValue)
                    .Select(x => x.DateTime.Value)
                    .OrderBy(x => x)
                    .ForEach(eventDates.Enqueue);
            }

            CalendarEntries.Clear();
            Random random = new Random();
            while (allEntries.Count > 0)
            {
                int indexToUse = random.Next(allEntries.Count);
                AbstractCalendarEntryViewModel entryToUse = allEntries[indexToUse];
                if (hasValidDates)
                {
                    entryToUse.DateTime = eventDates.Dequeue();
                }

                CalendarEntries.Add(entryToUse);
                allEntries.Remove(entryToUse);
            }

            RecalculateEventNumbers();
        }

        private DateTime GetNextRaceDay(DateTime dateTime)
        {
            if (!IsRaceDaySpecified())
            {
                return dateTime;
            }

            DayOfWeek requiredDayOfWeek = GetDayOfWeekOfNextEvent();
            return dateTime.DayOfWeek == requiredDayOfWeek ? dateTime : dateTime.Next(requiredDayOfWeek);
        }

        private bool IsRaceDaySpecified()
        {
            return SelectedRaceDay.Value != RaceDay.Any;
        }

        private DayOfWeek GetDayOfWeekOfNextEvent()
        {
            RaceDay selectedRaceDay = SelectedRaceDay.Value;
            return selectedRaceDay == RaceDay.Random ? (DayOfWeek)Random.Shared.Next(7) : RaceDayMap.GetDayOfWeek(selectedRaceDay);
        }
    }
}
namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar
{
    using System;
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public abstract class AbstractCalendarEntryViewModel : AbstractViewModel
    {
        private string _trackName;
        private int _eventNumber;

        private string _customEventName;
        private string _originalEventName;
        private DateTime? _dateTime;
        private bool _showDateTimePicker;
        private DateTime _calendarStart;
        private DateTime _calendarEnd;

        public int EventNumber
        {
            get => _eventNumber;
            set => SetProperty(ref _eventNumber, value);
        }

        public string TrackName
        {
            get => _trackName;
            set => SetProperty(ref _trackName, value);
        }

        public string CustomEventName
        {
            get => string.IsNullOrEmpty(_customEventName) ? _originalEventName : _customEventName;
            set => SetProperty(ref _customEventName, value);
        }

        public string OriginalEventName
        {
            get => _originalEventName;
            set => SetProperty(ref _originalEventName, value, nameof(CustomEventName));
        }

        public DateTime? DateTime
        {
            get => _dateTime;
            set => SetProperty(ref _dateTime, value);
        }

        public bool ShowDateTimePicker
        {
            get => _showDateTimePicker;
            set => SetProperty(ref _showDateTimePicker, value);
        }

        public DateTime CalendarStart
        {
            get => _calendarStart;
            set => SetProperty(ref _calendarStart, value);
        }

        public DateTime CalendarEnd
        {
            get => _calendarEnd;
            set => SetProperty(ref _calendarEnd, value);
        }

        public double LayoutLength { get; set; }

        public ICommand DeleteEntryCommand { get; set; }
    }
}
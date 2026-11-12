namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar.Predefined
{
    using System;

    using Common.Championship.Calendar;
    using SecondMonitor.ViewModels;

    public class CalendarTemplateViewModel : AbstractViewModel<CalendarTemplate>
    {
        private string _title;
        private int _calendarYear;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int CalendarYear
        {
            get => _calendarYear;
            set => SetProperty(ref _calendarYear, value);
        }

        protected override void ApplyModel(CalendarTemplate model)
        {
            Title = model.CalendarName;
            CalendarYear = model.Year;
        }

        public override CalendarTemplate SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}
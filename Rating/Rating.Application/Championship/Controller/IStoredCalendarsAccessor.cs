namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using Common.DataModel.Championship.Calendar;

    public interface IStoredCalendarsAccessor
    {
        bool TrySaveCalendar(CalendarDto calendarDto);

        bool TryLoadCalendar(out CalendarDto calendarDto);
    }
}
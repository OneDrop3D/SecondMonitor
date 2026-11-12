namespace SecondMonitor.Rating.Common.Championship.Calendar
{
    using System.Collections.Generic;
    using System.Linq;

    public class CalendarTemplate
    {
        public CalendarTemplate(string calendarName, int year, IEnumerable<EventTemplate> events)
        {
            CalendarName = calendarName;
            Year = year;
            Events = events.ToList();
        }

        public IReadOnlyList<EventTemplate> Events { get; }

        public string CalendarName { get; }
        public int Year { get; }

        public override string ToString()
        {
            return CalendarName;
        }
    }
}
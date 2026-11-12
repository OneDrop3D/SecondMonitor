namespace RemoteTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using SecondMonitor.Rating.Common.Championship.Calendar;
    using SecondMonitor.Rating.Common.Championship.Calendar.Templates.CalendarGroups;

    public class ChampionshipsCalendarTests
    {
        private List<CalendarTemplate> _allEvents;

        [SetUp]
        public void SetUp()
        {
            _allEvents = AllGroups.MainGroup.GetAllDescendantTemplates().ToList();
        }

        private static List<CalendarTemplate> _allCalendars = AllGroups.MainGroup.GetAllDescendantTemplates().ToList();

        [TestCaseSource(nameof(_allCalendars))]
        public void AllEvents_WhenEventHasName_ThenItIsProperlyTrimmed(CalendarTemplate calendarTemplate)
        {
            // Act & Assert
            Assert.That(calendarTemplate.CalendarName, Is.EqualTo(calendarTemplate.CalendarName.Trim()));

            foreach (EventTemplate eventTemplate in calendarTemplate.Events.Where(x => x.HasEventName))
            {
                Assert.That(eventTemplate.EventName, Is.EqualTo(eventTemplate.EventName.Trim()));
            }
        }

        private static List<CalendarTemplate> _allCalendarsWithDateEvents =
            AllGroups.MainGroup.GetAllDescendantTemplates().Where(x => x.Events.All(e => e.EventDate != null)).ToList();

        [TestCaseSource(nameof(_allCalendarsWithDateEvents))]
        public void AllEvents_WhenEventHasDatesDefined_ThenDatesFollowEachOther(CalendarTemplate calendarTemplate)
        {
            DateTime lastEventDate = new DateTime(calendarTemplate.Year, 1, 1);
            foreach (EventTemplate eventTemplate in calendarTemplate.Events)
            {
                Assert.That(eventTemplate.EventDate, Is.Not.Null);
                DateTime currentEventDateTime = eventTemplate.EventDate.Value;

                Assert.That(currentEventDateTime, Is.GreaterThan(lastEventDate));
                lastEventDate = currentEventDateTime;
            }
        }
    }
}

namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar
{
    using System.Collections.Generic;
    using Common.Championship.Calendar;
    using Common.DataModel.Championship.Calendar;

    public interface ICalendarEntryViewModelFactory
    {
        AbstractCalendarEntryViewModel Create(AbstractTrackTemplateViewModel trackTemplate);
        AbstractCalendarEntryViewModel Create(EventTemplate eventTemplate, string simulatorName, bool useCalendarEventNames, bool autoReplaceKnownTracks);

        IEnumerable<AbstractCalendarEntryViewModel> Create(CalendarDto calendarDto, string simulatorName);
    }
}
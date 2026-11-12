namespace SecondMonitor.Rating.Common.Championship.Calendar
{
    using System;
    using DataModel.Championship;

    public class EventTemplate
    {
        public EventTemplate(TrackTemplate trackTemplate) : this(trackTemplate, string.Empty)
        {
        }

        public EventTemplate(TrackTemplate trackTemplate, DateTime eventDate) : this(trackTemplate, string.Empty, eventDate)
        {
        }

        public EventTemplate(TrackTemplate trackTemplate, string eventName) : this(trackTemplate, eventName, null)
        {
        }

        public EventTemplate(TrackTemplate trackTemplate, string eventName, DateTime? eventDate)
        {
            TrackTemplate = trackTemplate;
            EventName = eventName;
            EventDate = eventDate;
        }

        public TrackTemplate TrackTemplate { get; }

        public string EventName { get; }
        public DateTime? EventDate { get; }

        public bool HasEventName => !string.IsNullOrWhiteSpace(EventName);
    }
}
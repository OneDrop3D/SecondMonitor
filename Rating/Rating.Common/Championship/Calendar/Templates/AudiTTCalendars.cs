namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public static class AudiTTCalendars
    {
        public static CalendarTemplate AudiTT2017 => new CalendarTemplate("2017 - Audi TT", 2017, new[]
        {
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.NurburgringGpPresent),
            new EventTemplate(TracksTemplates.NorisringPresent),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AudiTT2016 => new CalendarTemplate("2016 - Audi TT", 2016, new[]
        {
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.NurburgringGpPresent),
            new EventTemplate(TracksTemplates.NorisringPresent),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });
    }
}
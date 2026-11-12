namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class PorscheSuperCupCalendars
    {
        public static CalendarTemplateGroup AllPorscheSuperCupCalendars => new CalendarTemplateGroup("Porsche Supercup", new[] { PorscheSupercup2019Calendar, PorscheSupercup2018Calendar, PorscheSupercup1993Calendar });

        public static CalendarTemplate PorscheSupercup1993Calendar => new CalendarTemplate("Supercup 1993", 1993, new[]
        {
            new EventTemplate(TracksTemplates.ImolaGp8594),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGp9194),
            new EventTemplate(TracksTemplates.Monaco8696),
            new EventTemplate(TracksTemplates.NorisringPresent),
            new EventTemplate(TracksTemplates.MagnyCourseGp92to02),
            new EventTemplate(TracksTemplates.HockenheimringGp92to01),
            new EventTemplate(TracksTemplates.Hungaroring8902),
            new EventTemplate(TracksTemplates.Spa8393),
            new EventTemplate(TracksTemplates.HockenheimringGp92to01),
        });

        public static CalendarTemplate PorscheSupercup2019Calendar => new CalendarTemplate("Supercup 2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent),
            new EventTemplate(TracksTemplates.MonacoPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.MonzaGpPresent),
            new EventTemplate(TracksTemplates.MexicoGpPresent),
            new EventTemplate(TracksTemplates.MexicoGpPresent),
        });

        public static CalendarTemplate PorscheSupercup2018Calendar => new CalendarTemplate("Supercup 2018", 2018, new[]
        {
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent),
            new EventTemplate(TracksTemplates.MonacoPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.MonzaGpPresent),
            new EventTemplate(TracksTemplates.MexicoGpPresent),
            new EventTemplate(TracksTemplates.MexicoGpPresent),
        });
    }
}
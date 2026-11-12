namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class M1ProCarCalendars
    {
        public static CalendarTemplateGroup AllM1ProCarCalendars => new CalendarTemplateGroup("BMW M1 Procar Championship", new CalendarTemplate[] { BmwM11979 });

        public static CalendarTemplate BmwM11979 => new CalendarTemplate("1979 BMW M1 Procar Championship", 1979, new[]
        {
            new EventTemplate(TracksTemplates.ZolderGp7581),
            new EventTemplate(TracksTemplates.Monaco5571),
            new EventTemplate(TracksTemplates.DijonPrenoisGP7275),
            new EventTemplate(TracksTemplates.SilverstoneGp5274),
            new EventTemplate(TracksTemplates.HockenheimringGp7080),
            new EventTemplate(TracksTemplates.RedBullOsterreichring),
            new EventTemplate(TracksTemplates.Zandvoort4878),
            new EventTemplate(TracksTemplates.MonzaRoad5971),
        });
    }
}
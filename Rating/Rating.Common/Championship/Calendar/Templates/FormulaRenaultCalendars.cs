namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class FormulaRenaultCalendars
    {
        public static CalendarTemplateGroup AllFormulaRenaultCalendars => new CalendarTemplateGroup("Formula Renault 3.5", new CalendarTemplateGroup[] { FormulaRenaultCalendarEurope });

        public static CalendarTemplateGroup FormulaRenaultCalendarEurope => new CalendarTemplateGroup("Formula Renault 3.5 Series (Europe)", new CalendarTemplate[] { FiaGt1World2011 });

        public static CalendarTemplate FiaGt1World2011 => new CalendarTemplate("2015", 2015, new[]
        {
            new EventTemplate(TracksTemplates.MotorlandAragonGrandPrix),
            new EventTemplate(TracksTemplates.MonacoPresent),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.LeMansBugatti),
            new EventTemplate(TracksTemplates.JerezGpPresent),
        });
    }
}
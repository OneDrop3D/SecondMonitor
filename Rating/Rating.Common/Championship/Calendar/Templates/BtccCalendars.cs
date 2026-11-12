namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public static class BTCCCalendars
    {
        public static CalendarTemplateGroup AllBTCCCalendars => new CalendarTemplateGroup("BTCC", new CalendarTemplate[]
        {
            BTCC2022,
        });

        public static CalendarTemplate BTCC2022 => new CalendarTemplate("2022 - BTCC", 2022, new[]
        {
            new EventTemplate(TracksTemplates.DoningtonParkNational),
            new EventTemplate(TracksTemplates.BrandsHatchIndyPresent),
            new EventTemplate(TracksTemplates.Thruxton),
            new EventTemplate(TracksTemplates.OultonParkIslandCircuit),
            new EventTemplate(TracksTemplates.CroftMainCircuit),
            new EventTemplate(TracksTemplates.KnockhillInternational),
            new EventTemplate(TracksTemplates.Snetterton300),
            new EventTemplate(TracksTemplates.Thruxton),
            new EventTemplate(TracksTemplates.SilverstoneNationalPresent),
            new EventTemplate(TracksTemplates.BrandsHatchGpPresent),
        });
    }
}
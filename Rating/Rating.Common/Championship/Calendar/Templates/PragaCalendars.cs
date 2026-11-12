namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public static class PragaCalendars
    {
        public static CalendarTemplateGroup AllPragaCalendars => new CalendarTemplateGroup("Praga", new CalendarTemplate[]
        {
            PragaUk2022,
        });

        public static CalendarTemplate PragaUk2022 => new CalendarTemplate("2022 - Praga UK Cup", 2022, new[]
        {
            new EventTemplate(TracksTemplates.SilverstoneGpPresent),
            new EventTemplate(TracksTemplates.OultonParkInternationalCircuit),
            new EventTemplate(TracksTemplates.Snetterton300),
            new EventTemplate(TracksTemplates.SilverstoneNationalPresent),
            new EventTemplate(TracksTemplates.DoningtonParkNational),
            new EventTemplate(TracksTemplates.DoningtonPark),
        });
    }
}
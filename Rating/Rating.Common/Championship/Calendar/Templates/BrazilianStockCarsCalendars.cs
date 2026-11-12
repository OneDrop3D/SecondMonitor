namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class BrazilianStockCarsCalendars
    {
        public static CalendarTemplateGroup AllBrazilianStockCarsCalendars => new CalendarTemplateGroup("Brazil Stock Cars", new CalendarTemplate[] { BrazilStockCars2019 });

        public static CalendarTemplate BrazilStockCars2019 => new CalendarTemplate("2019 Stock Car Brazil Championship", 2019, new[]
        {
            new EventTemplate(TracksTemplates.Velopark),
            new EventTemplate(TracksTemplates.VeloCitta),
            new EventTemplate(TracksTemplates.Goiania),
            new EventTemplate(TracksTemplates.Londrina),
            new EventTemplate(TracksTemplates.SantaCruzdoSul),
            new EventTemplate(TracksTemplates.CampoGrande),
            new EventTemplate(TracksTemplates.InterlagosGpPresent),
            new EventTemplate(TracksTemplates.Velopark),
            new EventTemplate(TracksTemplates.Cascavel),
            new EventTemplate(TracksTemplates.VeloCitta),
            new EventTemplate(TracksTemplates.Goiania),
            new EventTemplate(TracksTemplates.InterlagosGpPresent),
        });
    }
}
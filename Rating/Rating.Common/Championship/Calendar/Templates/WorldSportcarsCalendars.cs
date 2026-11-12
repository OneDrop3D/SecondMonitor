namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class WorldSportcarsCalendars
    {
        public static CalendarTemplateGroup AllWorldSportcarsCalendars => new CalendarTemplateGroup("World Sportscar Championship", new[] { WorldSportscar1978Calendar, WorldSportscar1985Calendar, WorldSportscar1992Calendar });

        public static CalendarTemplate WorldSportscar1978Calendar => new CalendarTemplate("1978", 1978, new[]
        {
            new EventTemplate(TracksTemplates.DaytonaRoadCourse7583, "24 Hours of Daytona"),
            new EventTemplate(TracksTemplates.MugelloGp7490, "6 Hours of Mugello"),
            new EventTemplate(TracksTemplates.DijonPrenoisGPPresent, "6 Hours of Dijon"),
            new EventTemplate(TracksTemplates.SilverstoneGp7586, "6 Hours of Silverstone"),
            new EventTemplate(TracksTemplates.NordschleifeHistoric, "1000km Nürburgring"),
            new EventTemplate(TracksTemplates.MisanoWorldCircuit7282, "Misano 6 Hours"),
            new EventTemplate(TracksTemplates.WatkinsGlenGpWithoutInnerLoop, "Watkins Glen 6 Hours"),
            new EventTemplate(TracksTemplates.VallelungaInternational7186, "6 Hours of Vallelunga"),
        });

        public static CalendarTemplate WorldSportscar1985Calendar => new CalendarTemplate("1985", 1985, new[]
        {
            new EventTemplate(TracksTemplates.MugelloGp7490, "1000 km Mugello"),
            new EventTemplate(TracksTemplates.MonzaGp7694, "Trofeo Filippo Caracciolo (1000km)"),
            new EventTemplate(TracksTemplates.SilverstoneGp7586, "Silverstone 1000 Kilometres"),
            new EventTemplate(TracksTemplates.CircuitdelaSarthe7985, "24 Hours of Le Mans"),
            new EventTemplate(TracksTemplates.HockenheimringGp8291, "Duschfrish ADAC 1000 Kilometres"),
            new EventTemplate(TracksTemplates.CanadianTireMotosportPark, "Budweiser GT 1000 Kilometres"),
            new EventTemplate(TracksTemplates.Spa8393, "1000km of Spa"),
            new EventTemplate(TracksTemplates.BrandsHatchGp7687, "1000 km of Brands Hatch"),
            new EventTemplate(TracksTemplates.FujiGp8485, "Fuji 1000 Kilometres"),
            new EventTemplate(TracksTemplates.ShahAlamCircuit, "Malaysia 800 Selangor"),
        });

        public static CalendarTemplate WorldSportscar1992Calendar => new CalendarTemplate("1992", 1992, new[]
        {
            new EventTemplate(TracksTemplates.MonzaGp7694, "Trofeo F. Caracciolo (500 km)"),
            new EventTemplate(TracksTemplates.SilverstoneGp9193, "BRDC Empire Trophy (500 km)"),
            new EventTemplate(TracksTemplates.CircuitdelaSarthe9194, "24 Hours of Le Mans"),
            new EventTemplate(TracksTemplates.DoningtonParkGP8609, "Triton Showers Trophy (500 km)"),
            new EventTemplate(TracksTemplates.SuzukaGP9199, "Suzuka 1000km"),
            new EventTemplate(TracksTemplates.MagnyCourseGp92to02, "Championnat du Monde de Voitures de Sport (500 km)"),
        });
    }
}
namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class V8SupercarsCalendar
    {
        public static CalendarTemplateGroup AllV8SupercarsCalendars => new CalendarTemplateGroup("Supercars Championship", new[] { V8Supercars2019 });

        public static CalendarTemplate V8Supercars2019 => new CalendarTemplate("Supercars 2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.AdelaideGpPresent, "Adelaide 500"),
            new EventTemplate(TracksTemplates.AlbertPark1120, "Melbourne 400"),
            new EventTemplate(TracksTemplates.SymmonsPlains, "Tasmania SuperSprint"),
            new EventTemplate(TracksTemplates.PhillipIslandGp, "Phillip Island SuperSprint"),
            new EventTemplate(TracksTemplates.BarbagalloRacewaySupercars, "Perth SuperNight"),
            new EventTemplate(TracksTemplates.WintonMotorRacewayNational, "Winton SuperSprint"),
            new EventTemplate(TracksTemplates.HiddenValleyRaceway, "Darwin Triple Crown"),
            new EventTemplate(TracksTemplates.TownsvilleStreetCircuit, "Townsville 400"),
            new EventTemplate(TracksTemplates.QueenslandRaceway, "Ipswich SuperSprint"),
            new EventTemplate(TracksTemplates.BendMotorsportParkInternational, "The Bend SuperSprint"),
            new EventTemplate(TracksTemplates.PukekohePark, "Auckland SuperSprint"),
            new EventTemplate(TracksTemplates.BathurstPresent, "Bathurst 1000"),
            new EventTemplate(TracksTemplates.SurfersParadiseStreetCircuit, "Gold Coast 600"),
            new EventTemplate(TracksTemplates.SandownRaceway, "Sandown 500"),
            new EventTemplate(TracksTemplates.NewcastleStreetCircuit, "Newcastle 500"),
        });
    }
}
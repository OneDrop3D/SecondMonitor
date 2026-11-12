namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using System;

    using SecondMonitor.Rating.Common.Championship.Calendar.Tracks;

    public static class Gt2Calendars
    {
        public static CalendarTemplateGroup AllGt2Calendars => new CalendarTemplateGroup("GT2", new CalendarTemplateGroup[] { Gt2EuropeSeriesCalendars, GtAmericanSeriesCalendars });

        public static CalendarTemplateGroup Gt2EuropeSeriesCalendars => new CalendarTemplateGroup("GT2 European Series", new CalendarTemplate[] { Gt2EuropeanSeries2023 });

        public static CalendarTemplateGroup GtAmericanSeriesCalendars => new CalendarTemplateGroup("GT America Series", new CalendarTemplate[] { GtAmericanSeries2023 });

        public static CalendarTemplate Gt2EuropeanSeries2023 => new CalendarTemplate("2023", 2023, new EventTemplate[]
        {
            new EventTemplate(TracksTemplates.ImolaGpPresent, new DateTime(2023, 4, 23)),
            new EventTemplate(TracksTemplates.RedBullRing, new DateTime(2023, 5, 27)),
            new EventTemplate(TracksTemplates.DijonPrenoisGPPresent, new DateTime(2023, 6, 17)),
            new EventTemplate(TracksTemplates.AlgarveCircuit1Present, new DateTime(2023, 7, 23)),
            new EventTemplate(TracksTemplates.CircuitDeValencia, new DateTime(2023, 9, 17)),
            new EventTemplate(TracksTemplates.PaulRicard1AV2, new DateTime(2023, 10, 7))
        });

        public static CalendarTemplate GtAmericanSeries2023 => new CalendarTemplate("2023", 2023, new EventTemplate[]
        {
            new EventTemplate(TracksTemplates.StPetersburgPresent, new DateTime(2023, 3, 5)),
            new EventTemplate(TracksTemplates.SonomaRacewayLong, new DateTime(2023, 4, 2)),
            new EventTemplate(TracksTemplates.NolaMotosportPark, new DateTime(2023, 4, 30)),
            new EventTemplate(TracksTemplates.CotaGP, new DateTime(2023, 5, 21)),
            new EventTemplate(TracksTemplates.VirginiaIntRacewayFull, new DateTime(2023, 6, 18)),
            new EventTemplate(TracksTemplates.NashvilleStreetCircuit, new DateTime(2023, 8, 6)),
            new EventTemplate(TracksTemplates.RoadAmerica, new DateTime(2023, 8, 21)),
            new EventTemplate(TracksTemplates.SebringGpPresent, new DateTime(2023, 9, 25)),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, new DateTime(2023, 10, 9)),
        });
    }
}

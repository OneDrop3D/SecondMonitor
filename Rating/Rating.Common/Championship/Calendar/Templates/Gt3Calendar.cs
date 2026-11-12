namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using System;

    using Tracks;

    public static class Gt3Calendar
    {
        public static CalendarTemplateGroup AllGt3Calendar => new CalendarTemplateGroup("GT3", new CalendarTemplateGroup[] { IntercontinentalGtChallenge, GtWorldChallenge });

        public static CalendarTemplateGroup IntercontinentalGtChallenge => new CalendarTemplateGroup("Intercontinental GT Challenge",
            new CalendarTemplate[] { IntercontinentalGtChallenge2019, IntercontinentalGtChallenge2020, IntercontinentalGtChallenge2023 });

        public static CalendarTemplateGroup GtWorldChallenge => new CalendarTemplateGroup("Blancpain GT World Challenge",
            new CalendarTemplateGroup[] { GtWorldChallengeEuropeSprint, GtWorldChallengeEuropeEndurance, GtWorldChallengeAsia, GtWorldChallengeAmerica, GtWorldChallengeEurope });

        public static CalendarTemplateGroup GtWorldChallengeEuropeEndurance => new CalendarTemplateGroup("GT World Challenge Europe Endurance Cup",
            new CalendarTemplate[] { GtWorldChallengeEnduranceEurope2019, GtWorldChallengeEnduranceEurope2023, GtWorldChallengeEnduranceEurope2024 });

        public static CalendarTemplateGroup GtWorldChallengeEurope =>
            new CalendarTemplateGroup("GT World Challenge Europe", new CalendarTemplate[] { GtWorldChallengeEurope2023 });

        public static CalendarTemplateGroup GtWorldChallengeEuropeSprint =>
            new CalendarTemplateGroup("GT World Challenge Europe Sprint Cup", new CalendarTemplate[] { GtWorldChallengeEurope2019, GtWorldChallengeEurope2023Sprint });

        public static CalendarTemplateGroup GtWorldChallengeAsia =>
            new CalendarTemplateGroup("Blancpain GT World Challenge Asia", new CalendarTemplate[] { GtWorldChallengeAsia2019 });

        public static CalendarTemplateGroup GtWorldChallengeAmerica =>
            new CalendarTemplateGroup("Blancpain GT World Challenge America", new CalendarTemplate[] { GtWorldChallengeAmerica2019, GtWorldChallengeAmerica2023, GtWorldChallengeAmerica2024 });

        public static CalendarTemplate IntercontinentalGtChallenge2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.BathurstPresent, "Liqui Moly Bathurst 12 Hour"),
            new EventTemplate(TracksTemplates.LagunaSecaPresent, "California 8 Hours"),
            new EventTemplate(TracksTemplates.SpaPresent, "Total 24 Hours of Spa"),
            new EventTemplate(TracksTemplates.SuzukaGPPresent, "Suzuka 10 Hours"),
            new EventTemplate(TracksTemplates.KyalamiGPPresent, "Kyalami 9 Hours"),
        });

        public static CalendarTemplate IntercontinentalGtChallenge2020 => new CalendarTemplate("2020", 2020, new[]
        {
            new EventTemplate(TracksTemplates.BathurstPresent, "Liqui Moly Bathurst 12 Hour"),
            new EventTemplate(TracksTemplates.SpaPresent, "Total 24 Hours of Spa"),
            new EventTemplate(TracksTemplates.SuzukaGPPresent, "Suzuka 10 Hours"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, "Indianapolis 8 Hours"),
            new EventTemplate(TracksTemplates.KyalamiGPPresent, "Kyalami 9 Hours"),
        });

        public static CalendarTemplate IntercontinentalGtChallenge2023 => new CalendarTemplate("2023", 2023, new[]
        {
            new EventTemplate(TracksTemplates.BathurstPresent, "Bathurst 12 Hour", new DateTime(2023, 2, 5)),
            new EventTemplate(TracksTemplates.KyalamiGPPresent, "Kyalami 9 Hours", new DateTime(2023, 2, 25)),
            new EventTemplate(TracksTemplates.SpaPresent, "24 Hours of Spa", new DateTime(2023, 7, 1)),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, "Indianapolis 8 Hours", new DateTime(2023, 10, 7)),
            new EventTemplate(TracksTemplates.YasMarinaGrandPrix, "Gulf 12 Hours", new DateTime(2023, 12, 10)),
        });

        public static CalendarTemplate GtWorldChallengeEnduranceEurope2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.MonzaGpPresent, "3 Hours of Monza"),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent, "3 Hours of Silverstone"),
            new EventTemplate(TracksTemplates.PaulRicard1AV2, "Circuit Paul Ricard 1000kms"),
            new EventTemplate(TracksTemplates.SpaPresent, "Total 24 Hours of Spa"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent, "3 Hours of Barcelona"),
        });

        public static CalendarTemplate GtWorldChallengeEnduranceEurope2023 => new CalendarTemplate("2023", 2023, new[]
        {
            new EventTemplate(TracksTemplates.MonzaGpPresent, "3 Hours of Monza", new DateTime(2023, 4, 23)),
            new EventTemplate(TracksTemplates.PaulRicard1AV2, "Circuit Paul Ricard 1000 km", new DateTime(2023, 6, 4)),
            new EventTemplate(TracksTemplates.SpaPresent, "CrowdStrike 24 Hours of Spa", new DateTime(2023, 7, 1)),
            new EventTemplate(TracksTemplates.NurburgringGpPresent, "3 Hours of Nürburgring", new DateTime(2023, 7, 30)),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent, "3 Hours of Barcelona", new DateTime(2023, 10, 1))
        });

        public static CalendarTemplate GtWorldChallengeEnduranceEurope2024 => new CalendarTemplate("2024", 2024, new[]
        {
            new EventTemplate(TracksTemplates.PaulRicard1AV2, "Circuit Paul Ricard 500 km", new DateTime(2024, 4, 7)),
            new EventTemplate(TracksTemplates.SpaPresent, "CrowdStrike 24 Hours of Spa", new DateTime(2024, 6, 29)),
            new EventTemplate(TracksTemplates.NurburgringGpPresent, "3 Hours of Nürburgring", new DateTime(2024, 7, 28)),
            new EventTemplate(TracksTemplates.MonzaGpPresent, "3 Hours of Monza", new DateTime(2024, 9, 22)),
            new EventTemplate(TracksTemplates.JeddahCornicheCircuit, "Jeddah Corniche 1000 km", new DateTime(2024, 11, 23))
        });

        public static CalendarTemplate GtWorldChallengeEurope2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.BrandsHatchGpPresent),
            new EventTemplate(TracksTemplates.MisanoWorldCircuitPresent),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.NurburgringGpPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
        });

        public static CalendarTemplate GtWorldChallengeEurope2023Sprint => new CalendarTemplate("2023", 2023, new[]
        {
            new EventTemplate(TracksTemplates.BrandsHatchGpPresent, new DateTime(2023, 5, 14)),
            new EventTemplate(TracksTemplates.MisanoWorldCircuitPresent, new DateTime(2023, 7, 16)),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent, new DateTime(2023, 9, 3)),
            new EventTemplate(TracksTemplates.CircuitDeValencia, new DateTime(2023, 9, 17)),
            new EventTemplate(TracksTemplates.ZandvoortGPPresent, new DateTime(2023, 10, 15)),
        });

        public static CalendarTemplate GtWorldChallengeEurope2023 => new CalendarTemplate("2023", 2023, new[]
        {
            new EventTemplate(TracksTemplates.MonzaGpPresent, new DateTime(2023, 4, 23)),
            new EventTemplate(TracksTemplates.BrandsHatchGpPresent, new DateTime(2023, 5, 14)),
            new EventTemplate(TracksTemplates.PaulRicard1AV2, new DateTime(2023, 6, 4)),
            new EventTemplate(TracksTemplates.SpaPresent, new DateTime(2023, 7, 1)),
            new EventTemplate(TracksTemplates.MisanoWorldCircuitPresent, new DateTime(2023, 7, 16)),
            new EventTemplate(TracksTemplates.NurburgringGpPresent, new DateTime(2023, 7, 30)),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent, new DateTime(2023, 9, 3)),
            new EventTemplate(TracksTemplates.CircuitDeValencia, new DateTime(2023, 9, 17)),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent, new DateTime(2023, 10, 1)),
            new EventTemplate(TracksTemplates.ZandvoortGPPresent, new DateTime(2023, 10, 15)),
        });

        public static CalendarTemplate GtWorldChallengeAsia2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.SepangGPPresent),
            new EventTemplate(TracksTemplates.ChangGp),
            new EventTemplate(TracksTemplates.SuzukaGPPresent),
            new EventTemplate(TracksTemplates.FujiGpPresent),
            new EventTemplate(TracksTemplates.KoreaGp),
            new EventTemplate(TracksTemplates.ShanghaiGp),
        });

        public static CalendarTemplate GtWorldChallengeAmerica2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.CotaGP),
            new EventTemplate(TracksTemplates.VirginiaIntRacewayFull),
            new EventTemplate(TracksTemplates.CanadianTireMotosportPark),
            new EventTemplate(TracksTemplates.SonomaRacewayLong),
            new EventTemplate(TracksTemplates.WatkinsGlenGpWithInnerLoop),
            new EventTemplate(TracksTemplates.RoadAmerica),
            new EventTemplate(TracksTemplates.LasVegasCombinedLayout),
        });

        public static CalendarTemplate GtWorldChallengeAmerica2023 => new CalendarTemplate("2023", 2023, new[]
        {
            new EventTemplate(TracksTemplates.SonomaRacewayLong, new DateTime(2023, 4, 2)),
            new EventTemplate(TracksTemplates.NolaMotosportPark, new DateTime(2023, 4, 30)),
            new EventTemplate(TracksTemplates.CotaGP, new DateTime(2023, 5, 21)),
            new EventTemplate(TracksTemplates.VirginiaIntRacewayFull, new DateTime(2023, 6, 18)),
            new EventTemplate(TracksTemplates.RoadAmerica, new DateTime(2023, 8, 20)),
            new EventTemplate(TracksTemplates.SebringGpPresent, new DateTime(2023, 9, 24)),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, new DateTime(2023, 10, 8))
        });

        public static CalendarTemplate GtWorldChallengeAmerica2024 => new CalendarTemplate("2024", 2024, new[]
        {
            new EventTemplate(TracksTemplates.SonomaRacewayLong, new DateTime(2024, 4, 7)),
            new EventTemplate(TracksTemplates.SebringGpPresent, new DateTime(2024, 5, 5)),
            new EventTemplate(TracksTemplates.CotaGP, new DateTime(2024, 5, 19)),
            new EventTemplate(TracksTemplates.VirginiaIntRacewayFull, new DateTime(2024, 7, 21)),
            new EventTemplate(TracksTemplates.RoadAmerica, new DateTime(2024, 8, 18)),
            new EventTemplate(TracksTemplates.BarberMotorsportsPark, new DateTime(2024, 9, 8)),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, new DateTime(2024, 10, 6))
        });
    }
}

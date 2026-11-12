namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public static class IndycarCalendars
    {
        public static CalendarTemplateGroup AllIndyCar => new CalendarTemplateGroup("IndyCar", new CalendarTemplate[] { IndyCar2023, IndyCar2019, IndyCar2016 });

        public static CalendarTemplate IndyCar2016 => new CalendarTemplate("2016 - IndyCar", 2016, new[]
        {
            new EventTemplate(TracksTemplates.StPetersburg0316, "Firestone Grand Prix of St. Petersburg"),
            new EventTemplate(TracksTemplates.PhoenixInternationalRaceway, "Desert Diamond West Valley Phoenix Grand Prix"),
            new EventTemplate(TracksTemplates.LongBeach, "42nd Toyota Grand Prix of Long Beach"),
            new EventTemplate(TracksTemplates.BarberMotorsportsPark, "Honda Indy Grand Prix of Alabama"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, "Angie's List Grand Prix of Indianapolis"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayOval, "100th Indianapolis 500 presented by PennGrade"),
            new EventTemplate(TracksTemplates.DetroitBelleIsle, "Chevrolet Dual in Detroit Presented by Quicken Loan"),
            new EventTemplate(TracksTemplates.DetroitBelleIsle, "Chevrolet Dual in Detroit Presented by Quicken Loan"),
            new EventTemplate(TracksTemplates.RoadAmerica, "Kohler Grand Prix"),
            new EventTemplate(TracksTemplates.IowaSpeedwayOval, "Iowa Corn 300"),
            new EventTemplate(TracksTemplates.TorontoExhibitionPlace, "Honda Indy Toronto"),
            new EventTemplate(TracksTemplates.MidOhioMainWithoutChicane, "Honda Indy 200"),
            new EventTemplate(TracksTemplates.PoconoRacewayOval, "ABC Supply 500"),
            new EventTemplate(TracksTemplates.TexasMotorSpeedwayOval, "Firestone 600"),
            new EventTemplate(TracksTemplates.WatkinsGlenGpWithInnerLoop, "Grand Prix at The Glen"),
            new EventTemplate(TracksTemplates.SonomaRacewayIndy, "GoPro Grand Prix of Sonoma"),
        });

        public static CalendarTemplate IndyCar2019 => new CalendarTemplate("2019 - IndyCar", 2019, new[]
        {
            new EventTemplate(TracksTemplates.StPetersburgPresent, "Firestone Grand Prix of St. Petersburg"),
            new EventTemplate(TracksTemplates.CotaGP, "IndyCar Classic"),
            new EventTemplate(TracksTemplates.BarberMotorsportsPark, "Honda Indy Grand Prix of Alabama Presented by AmFirst"),
            new EventTemplate(TracksTemplates.LongBeach, "Acura Grand Prix of Long Beach"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, "IndyCar Grand Prix"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayOval, "103rd Running of the Indianapolis 500 Presented by Gainbridge"),
            new EventTemplate(TracksTemplates.DetroitBelleIsle, "Chevrolet Detroit Grand Prix Presented by Lear Corporation"),
            new EventTemplate(TracksTemplates.DetroitBelleIsle, "Chevrolet Detroit Grand Prix Presented by Lear Corporation"),
            new EventTemplate(TracksTemplates.TexasMotorSpeedwayOval, "DXC Technology 600"),
            new EventTemplate(TracksTemplates.RoadAmerica, "REV Group Grand Prix at Road America"),
            new EventTemplate(TracksTemplates.TorontoExhibitionPlace, "Honda Indy Toronto"),
            new EventTemplate(TracksTemplates.IowaSpeedwayOval, "Iowa Corn 300"),
            new EventTemplate(TracksTemplates.MidOhioMainWithoutChicane, "Honda Indy 200 at Mid-Ohio"),
            new EventTemplate(TracksTemplates.PoconoRacewayOval, "ABC Supply 500"),
            new EventTemplate(TracksTemplates.WorldWideTechnologyRacewayGatewayOval, "Bommarito Automotive Group 500 Presented by Axalta and Valvoline"),
            new EventTemplate(TracksTemplates.PortlandInternationalRacewayPresent, "Grand Prix of Portland"),
            new EventTemplate(TracksTemplates.LagunaSecaPresent, "Firestone Grand Prix of Monterey"),
        });

        public static CalendarTemplate IndyCar2023 => new CalendarTemplate("2023 - IndyCar", 2023, new[]
        {
            new EventTemplate(TracksTemplates.StPetersburgPresent, "Streets of St. Petersburg"),
            new EventTemplate(TracksTemplates.TexasMotorSpeedwayOval, "Texas Motor Speedway"),
            new EventTemplate(TracksTemplates.LongBeach, "Streets of Long Beach"),
            new EventTemplate(TracksTemplates.BarberMotorsportsPark, "Barber Motorsports Park"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, "Indianapolis Motor Speedway (Road Course)"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayOval, "The 107th Indianapolis 500"),
            new EventTemplate(TracksTemplates.DetroitBelleIsle, "Streets of Detroit"),
            new EventTemplate(TracksTemplates.RoadAmerica, "Road America"),
            new EventTemplate(TracksTemplates.MidOhioMainWithoutChicane, "Mid-Ohio Sports Car Course"),
            new EventTemplate(TracksTemplates.TorontoExhibitionPlace, "Streets of Toronto"),
            new EventTemplate(TracksTemplates.IowaSpeedwayOval, "Iowa Speedway Race 1"),
            new EventTemplate(TracksTemplates.IowaSpeedwayOval, "Iowa Speedway Race 2"),
            new EventTemplate(TracksTemplates.NashvilleStreetCircuit, "Streets of Nashville"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoadPresent, "Indianapolis Motor Speedway (Road Course)"),
            new EventTemplate(TracksTemplates.WorldWideTechnologyRacewayGatewayOval, "World Wide Technology Raceway"),
            new EventTemplate(TracksTemplates.PortlandInternationalRacewayPresent, "Portland International Raceway"),
            new EventTemplate(TracksTemplates.LagunaSecaPresent, "WeatherTech Raceway Laguna Seca"),
        });
    }
}
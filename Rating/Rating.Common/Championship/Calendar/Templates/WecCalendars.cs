namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using System;
    using Tracks;

    public class WecCalendars
    {
        public static CalendarTemplateGroup AllWecCalendars => new CalendarTemplateGroup("World Endurance Championship", new[] { Wec1920Calendar, Wec2024Calendar, Wec2025Calendar });

        public static CalendarTemplate Wec1920Calendar => new CalendarTemplate("WEC 2019 - 2020", 2019, new[]
        {
            new EventTemplate(TracksTemplates.SilverstoneGpPresent, "4 Hours of Silverstone"),
            new EventTemplate(TracksTemplates.FujiGpPresent, "6 Hours of Fuji"),
            new EventTemplate(TracksTemplates.ShanghaiGp, "4 Hours of Shanghai"),
            new EventTemplate(TracksTemplates.BahrainGP, "Bapco 8 Hours of Bahrain"),
            new EventTemplate(TracksTemplates.CotaGP, "Lone Star Le Mans (6 hours)"),
            new EventTemplate(TracksTemplates.SpaPresent, "6 Hours of Spa-Francorchamps"),
            new EventTemplate(TracksTemplates.CircuitdelaSarthePresent, "24 Hours of Le Mans"),
        });

        public static CalendarTemplate Wec2024Calendar => new CalendarTemplate("WEC 2024", 2024, new[]
        {
            new EventTemplate(TracksTemplates.Qatar, "Qatar 1812 km", new DateTime(2024, 3, 2)),
            new EventTemplate(TracksTemplates.ImolaGpPresent, "6 Hours of Imola", new DateTime(2024, 4, 21)),
            new EventTemplate(TracksTemplates.SpaPresent, "6 Hours of Spa-Francorchamps", new DateTime(2024, 5, 11)),
            new EventTemplate(TracksTemplates.CircuitdelaSarthePresent, "24 Hours of Le Mans", new DateTime(2024, 6, 15)),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "6 Hours of São Paulo", new DateTime(2024, 7, 6)),
            new EventTemplate(TracksTemplates.CotaGP, "Lone Star Le Mans", new DateTime(2024, 9, 1)),
            new EventTemplate(TracksTemplates.FujiGpPresent, "6 Hours of Fuji", new DateTime(2024, 9, 15)),
            new EventTemplate(TracksTemplates.BahrainGP, "8 Hours of Bahrain", new DateTime(2024, 11, 2)),
        });

        public static CalendarTemplate Wec2025Calendar => new CalendarTemplate("WEC 2025", 2025, new[]
        {
            new EventTemplate(TracksTemplates.Qatar, "Qatar 1812 km", new DateTime(2025, 2, 28)),
            new EventTemplate(TracksTemplates.ImolaGpPresent, "6 Hours of Imola", new DateTime(2025, 4, 20)),
            new EventTemplate(TracksTemplates.SpaPresent, "6 Hours of Spa-Francorchamps", new DateTime(2025, 5, 10)),
            new EventTemplate(TracksTemplates.CircuitdelaSarthePresent, "24 Hours of Le Mans", new DateTime(2025, 6, 14)),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "6 Hours of São Paulo", new DateTime(2025, 7, 13)),
            new EventTemplate(TracksTemplates.CotaGP, "Lone Star Le Mans", new DateTime(2025, 9, 7)),
            new EventTemplate(TracksTemplates.FujiGpPresent, "6 Hours of Fuji", new DateTime(2025, 9, 28)),
            new EventTemplate(TracksTemplates.BahrainGP, "8 Hours of Bahrain", new DateTime(2025, 11, 8)),
        });
    }
}
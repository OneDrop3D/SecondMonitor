namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates.CalendarGroups
{
    using System.Linq;
    using Tracks;

    public static class TCRGroups
    {
        public static CalendarTemplateGroup AllTCRGroups => new CalendarTemplateGroup("TCR", new[] { WTCRGroup, TCRAsia, TCREurope }, Enumerable.Empty<CalendarTemplate>());

        public static CalendarTemplateGroup WTCRGroup => new CalendarTemplateGroup("WTCR", Enumerable.Empty<CalendarTemplateGroup>(), new[] { WTCRCalendars.WTCR2018, WTCRCalendars.WTCR2019 });

        public static CalendarTemplateGroup TCRAsia => new CalendarTemplateGroup("TCR Asia", Enumerable.Empty<CalendarTemplateGroup>(), new[] { TCRAsia2019 });

        public static CalendarTemplateGroup TCREurope => new CalendarTemplateGroup("TCR Europe", Enumerable.Empty<CalendarTemplateGroup>(), new[] { TCREurope2019 });

        public static CalendarTemplate TCRAsia2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.SepangGPPresent),
            new EventTemplate(TracksTemplates.ZhuhaiGp),
            new EventTemplate(TracksTemplates.ShanghaiGp),
            new EventTemplate(TracksTemplates.ZhejiangFullCircuit),
            new EventTemplate(TracksTemplates.BangsaenStreetCircuit),
        });

        public static CalendarTemplate TCREurope2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent),
            new EventTemplate(TracksTemplates.MonzaGpPresent),
        });
    }
}
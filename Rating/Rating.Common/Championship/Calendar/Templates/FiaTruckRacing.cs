namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public static class FiaTruckRacing
    {
        public static CalendarTemplateGroup AllFiaTruckRacing => new CalendarTemplateGroup("European Truck Racing Championship", new[] { FiaTrucks2019 });

        public static CalendarTemplate FiaTrucks2019 => new CalendarTemplate("2019", 2019, new[]
        {
            new EventTemplate(TracksTemplates.MisanoWorldCircuitPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.SlovakiaRingTrack4),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.AutodromMost),
            new EventTemplate(TracksTemplates.ZolderGpPresent),
            new EventTemplate(TracksTemplates.LeMansBugatti),
            new EventTemplate(TracksTemplates.JaramaGpPresent),
        });
    }
}
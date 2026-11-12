namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public class AdacGTMasterCalendars
    {
        public static CalendarTemplateGroup AllCalendars => new CalendarTemplateGroup("ADAC GT Masters",
            new[]
            {
                AdacGTMasters2013, AdacGTMasters2014, AdacGTMasters2015,
                AdacGTMasters2016, AdacGTMasters2017, AdacGTMasters2018, AdacGTMasters2019,
                AdacGTMasters2020, AdacGTMasters2021
            });

        public static CalendarTemplate AdacGTMasters2013 => new CalendarTemplate("2013 - ADAC GT Masters", 2013, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.SlovakiaRingTrack4),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AdacGTMasters2014 => new CalendarTemplate("2014 - ADAC GT Masters", 2014, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.SlovakiaRingTrack4),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AdacGTMasters2015 => new CalendarTemplate("2015 - ADAC GT Masters", 2015, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AdacGTMasters2016 => new CalendarTemplate("2016 - ADAC GT Masters", 2016, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AdacGTMasters2017 => new CalendarTemplate("2017 - ADAC GT Masters", 2017, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AdacGTMasters2018 => new CalendarTemplate("2018 - ADAC GT Masters", 2018, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.AutodromMost),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });

        public static CalendarTemplate AdacGTMasters2019 => new CalendarTemplate("2019 - ADAC GT Masters", 2019, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.AutodromMost),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.NurburgringSprintPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
        });

        public static CalendarTemplate AdacGTMasters2020 => new CalendarTemplate("2020 - ADAC GT Masters", 2020, new[]
        {
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.NurburgringGpPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.Oschersleben),
        });

        public static CalendarTemplate AdacGTMasters2021 => new CalendarTemplate("2021 - ADAC GT Masters", 2021, new[]
        {
            new EventTemplate(TracksTemplates.Oschersleben),
            new EventTemplate(TracksTemplates.RedBullRing),
            new EventTemplate(TracksTemplates.ZandvoortGP9919),
            new EventTemplate(TracksTemplates.NurburgringGpPresent),
            new EventTemplate(TracksTemplates.LausitzringGpPresent),
            new EventTemplate(TracksTemplates.SachsenringPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
        });
    }
}
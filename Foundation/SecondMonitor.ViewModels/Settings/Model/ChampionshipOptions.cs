namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;

    using SecondMonitor.DataModel.Championship;

    public class ChampionshipOptions
    {
        public DateTime StartOfCalendar { get; set; } = new DateTime(DateTime.Now.Year, 3, 1);
        public DateTime EndOfCalendar { get; set; } = new DateTime(DateTime.Now.Year, 11, 30);

        public ScoringKind ManufacturingScoring { get; set; } = ScoringKind.F1Fair;

        public ScoringKind TeamScoring { get; set; } = ScoringKind.F1Fair;

        public RaceDay RaceDay { get; set; } = RaceDay.Sunday;
        public bool IsTrackAllClassesEnabled { get; set; } = false;
    }
}

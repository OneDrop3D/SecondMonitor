namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.BasicProperties;

    public class PitStopTimeDisplayMap : AbstractHumanReadableMap<PitStopTimeDisplayKind>
    {
        public PitStopTimeDisplayMap()
        {
            Translations.Add(PitStopTimeDisplayKind.FullStop, "Entry to Exit");
            Translations.Add(PitStopTimeDisplayKind.PitStall, "In Pit Stall");
            Translations.Add(PitStopTimeDisplayKind.Both, "Combined");
        }
    }
}
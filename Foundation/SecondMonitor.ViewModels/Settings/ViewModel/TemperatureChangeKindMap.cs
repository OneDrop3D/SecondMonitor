namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.Contracts;
    using SecondMonitor.ViewModels.Settings.Model;

    public class TemperatureChangeKindMap : AbstractHumanReadableMap<TemperatureChangeKind>
    {
        public TemperatureChangeKindMap()
        {
            Translations.Add(TemperatureChangeKind.Last5Minutes, "Last 5 Minutes");
            Translations.Add(TemperatureChangeKind.FromSessionStart, "From Session Start");
            Translations.Add(TemperatureChangeKind.FromStintStart, "From Stint Start");
        }
    }
}

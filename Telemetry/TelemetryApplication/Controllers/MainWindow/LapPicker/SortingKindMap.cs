namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.LapPicker
{
    using Contracts;

    public class SortingKindMap : AbstractHumanReadableMap<SortingKind>
    {
        public SortingKindMap()
        {
            Translations.Add(SortingKind.PlayerFirstByTime, "Player First, then Lap Time");
            Translations.Add(SortingKind.PlayerFirstByLap, "Player First, then Lap Num");
            Translations.Add(SortingKind.ByTime, "Lap Time");
            Translations.Add(SortingKind.ByLap, "Lap Num");
            Translations.Add(SortingKind.GroupByDriverTime, "Group By Driver, then Lap Time");
            Translations.Add(SortingKind.GroupByDriverByLap, "Group By Driver, then Lap Num");
        }
    }
}
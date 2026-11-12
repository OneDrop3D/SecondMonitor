namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.Contracts;
    using SecondMonitor.ViewModels.Settings.Model;

    public class LapReferenceKindMap : AbstractHumanReadableMap<LapReferenceKind>
    {
        public LapReferenceKindMap()
        {
            Translations.Add(LapReferenceKind.None, "None");
            Translations.Add(LapReferenceKind.SessionOverallBest, "Best Overall Lap");
            Translations.Add(LapReferenceKind.SessionClassBest, "Own Class Best Lap");
            Translations.Add(LapReferenceKind.PlayerBest, "Player's Best Lap");
            Translations.Add(LapReferenceKind.LastLap, "Player's Last Lap");
        }
    }
}

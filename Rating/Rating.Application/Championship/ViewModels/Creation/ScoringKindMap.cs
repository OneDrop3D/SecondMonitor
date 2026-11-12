namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Championship;

    public class ScoringKindMap : AbstractHumanReadableMap<ScoringKind>
    {
        public ScoringKindMap()
        {
            Translations.Add(ScoringKind.F1Fair, "F1 Style Scoring, Equalized");
            Translations.Add(ScoringKind.F1, "F1 Style Scoring");
            Translations.Add(ScoringKind.Imsa, "IMSA Style Scoring");
        }
    }
}

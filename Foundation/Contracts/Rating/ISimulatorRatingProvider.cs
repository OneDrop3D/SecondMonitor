namespace SecondMonitor.Contracts.Rating
{
    public interface ISimulatorRatingProvider
    {
        bool TryGetSuggestedDifficulty(string simulatorName, string className, out int difficulty);
    }
}
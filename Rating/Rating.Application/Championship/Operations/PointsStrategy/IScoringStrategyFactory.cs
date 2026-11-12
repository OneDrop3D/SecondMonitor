namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    using SecondMonitor.DataModel.Championship;

    public interface IScoringStrategyFactory
    {
        IScoringStrategy Create(ScoringKind scoringKind);
    }
}

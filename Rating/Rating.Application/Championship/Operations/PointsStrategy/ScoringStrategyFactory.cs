namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    using System;

    using Ninject.Syntax;

    using SecondMonitor.Contracts.NInject;
    using SecondMonitor.DataModel.Championship;

    public class ScoringStrategyFactory : AbstractBindingMetadataFactory<IScoringStrategy>, IScoringStrategyFactory
    {
        public ScoringStrategyFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
        }

        protected override string MetadataName => BindingMetadataIds.ChampionshipPointStrategy;

        protected override IScoringStrategy CreateEmpty()
        {
            throw new NotSupportedException();
        }

        public IScoringStrategy Create(ScoringKind scoringKind) => Create(scoringKind.ToString());
    }
}

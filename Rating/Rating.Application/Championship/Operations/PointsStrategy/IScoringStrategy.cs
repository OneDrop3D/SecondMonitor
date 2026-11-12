namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    using System.Collections.Generic;

    using SecondMonitor.Rating.Common.DataModel.Championship;

    public interface IScoringStrategy
    {
        public List<ParticipantPoints> ApplyScoring(List<RepresentantFinishPosition> finishPositions, ScoringDto scoringDto);
    }
}

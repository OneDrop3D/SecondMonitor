namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;

    public class F1ScoringStrategy : IScoringStrategy
    {
        public List<ParticipantPoints> ApplyScoring(List<RepresentantFinishPosition> finishPositions, ScoringDto scoringDto)
        {
           List<IGrouping<string, RepresentantFinishPosition>> finishPositionsGrouped = finishPositions.OrderBy(x => x.FinishPosition).GroupBy(x => x.Name).ToList();
           int totalParticipant = finishPositionsGrouped.Count(x => !string.IsNullOrEmpty(x.Key));
           if (totalParticipant < 2)
           {
               return [];
           }

           List<ParticipantPoints> participantPointsList = finishPositionsGrouped.Where(x => !string.IsNullOrEmpty(x.Key))
               .Select(p => new ParticipantPoints(p.Key, p.Sum(x => scoringDto.GetPointsForPosition(x.FinishPosition)))).ToList();

           return participantPointsList;
        }
    }
}

namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;

    public class ImsaScoringStrategy : IScoringStrategy
    {
        public List<ParticipantPoints> ApplyScoring(List<RepresentantFinishPosition> finishPositions, ScoringDto scoringDto)
        {
            List<IGrouping<string, RepresentantFinishPosition>> finishPositionsGrouped = finishPositions.OrderBy(x => x.FinishPosition).GroupBy(x => x.Name).ToList();
            int totalParticipant = finishPositionsGrouped.Count(x => !string.IsNullOrEmpty(x.Key));
            if (totalParticipant < 2)
            {
                return [];
            }

            List<RepresentantFinishPosition> representantByBestPositions =
                finishPositionsGrouped.Where(x => !string.IsNullOrEmpty(x.Key)).Select(x => x.First()).OrderBy(x => x.FinishPosition).ToList();

            List<ParticipantPoints> participantPointsList =
                representantByBestPositions.Select((x, i) => new ParticipantPoints(x.Name, scoringDto.GetPointsForPosition(i + 1))).ToList();

            return participantPointsList;
        }
    }
}
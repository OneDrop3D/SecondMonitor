namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public abstract class ParticipantPositionComparer<TParticipant> : IComparer<TParticipant> where TParticipant : ParticipantSessionResultDto
    {
        protected ParticipantPositionComparer(ChampionshipDto championshipDto, SessionResultDto currentSessionResult)
        {
            Results = championshipDto.GetAllResults().Concat([currentSessionResult]).SelectMany(x => GetResults(x)).ToList();
        }

        protected List<TParticipant> Results { get; }

        public int Compare(TParticipant x, TParticipant y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null || y == null)
            {
                return -1;
            }

            if (x.TotalPoints != y.TotalPoints)
            {
                //inverse comparison - more position occurrences means that the driver is "lower"
                return y.TotalPoints.CompareTo(x.TotalPoints);
            }

            for (int i = 1; i < 99; i++)
            {
                int driverXPositionCount = GetPositionsCount(i, x);
                int driverYPositionCount = GetPositionsCount(i, y);
                if (driverXPositionCount != driverYPositionCount)
                {
                    //inverse comparison - more position occurrences means that the driver is "lower"
                    return driverYPositionCount.CompareTo(driverXPositionCount);
                }
            }

            return 0;
        }

        protected abstract IEnumerable<TParticipant> GetResults(SessionResultDto sessionResultDto);

        protected abstract int GetPositionsCount(int position, TParticipant participant);
    }
}
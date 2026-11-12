namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class ClassDriverResultComparer : IComparer<ClassDriverResultDto>
    {
        public ClassDriverResultComparer(ChampionshipDto championshipDto, SessionResultDto currentSessionResult)
        {
            Results = championshipDto.GetAllResults().Concat([currentSessionResult]).SelectMany(x => GetResults(x)).ToList();
        }

        protected List<DriverSessionResultDto> Results { get; }

        public int Compare(ClassDriverResultDto x, ClassDriverResultDto y)
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

        protected IEnumerable<DriverSessionResultDto> GetResults(SessionResultDto sessionResultDto)
        {
            return sessionResultDto.DriverSessionResult;
        }

        protected int GetPositionsCount(int position, ClassDriverResultDto participant)
        {
            return Results.Count(x => x.ClassFinishPosition == position && x.DriverGuid == participant.DriverGlobalKey);
        }
    }
}

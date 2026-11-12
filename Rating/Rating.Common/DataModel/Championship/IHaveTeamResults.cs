namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System.Collections.Generic;

    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public interface IHaveTeamResults
    {
        public List<TeamSessionResultDto> TeamSessionResult { get; }
    }
}

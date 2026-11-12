namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class TeamsResultComparer : ParticipantPositionComparer<TeamSessionResultDto>
    {
        public TeamsResultComparer(ChampionshipDto championshipDto, SessionResultDto currentSessionResult) : base(championshipDto, currentSessionResult)
        {
        }

        protected override IEnumerable<TeamSessionResultDto> GetResults(SessionResultDto sessionResultDto)
        {
            return sessionResultDto.TeamSessionResult;
        }

        protected override int GetPositionsCount(int position, TeamSessionResultDto participant)
        {
            return Results.Count(x => x.FinishPosition == position && x.TeamName == participant.TeamName);
        }
    }
}

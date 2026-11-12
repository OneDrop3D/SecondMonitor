namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;

    public class DriverSessionResultComparer : ParticipantPositionComparer<DriverSessionResultDto>
    {
        public DriverSessionResultComparer(ChampionshipDto championshipDto, SessionResultDto currentSessionResult) : base(championshipDto, currentSessionResult)
        {
        }

        protected override IEnumerable<DriverSessionResultDto> GetResults(SessionResultDto sessionResultDto)
        {
            return sessionResultDto.DriverSessionResult;
        }

        protected override int GetPositionsCount(int position, DriverSessionResultDto participant)
        {
            return Results.Count(x => x.FinishPosition == position && x.DriverGuid == participant.DriverGuid);
        }
    }
}
namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class ManufacturerResultComparer : ParticipantPositionComparer<ManufacturerSessionResultDto>
    {
        public ManufacturerResultComparer(ChampionshipDto championshipDto, SessionResultDto currentSessionResult) : base(championshipDto, currentSessionResult)
        {
        }

        protected override IEnumerable<ManufacturerSessionResultDto> GetResults(SessionResultDto sessionResultDto)
        {
            return sessionResultDto.ManufacturersSessionResult;
        }

        protected override int GetPositionsCount(int position, ManufacturerSessionResultDto participant)
        {
            return Results.Count(x => x.FinishPosition == position && x.ManufacturerName == participant.ManufacturerName);
        }
    }
}

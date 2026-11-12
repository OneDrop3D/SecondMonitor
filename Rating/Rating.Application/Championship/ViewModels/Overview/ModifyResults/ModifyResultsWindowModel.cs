namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults
{
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class ModifyResultsWindowModel
    {
        public ModifyResultsWindowModel(ChampionshipDto championship, EventDto eventDto, SessionDto sessionDto, ScoringDto scoringDto)
        {
            Championship = championship;
            EventDto = eventDto;
            SessionDto = sessionDto;
            ScoringDto = scoringDto;
        }

        public ChampionshipDto Championship { get; init; }

        public EventDto EventDto { get; init; }
        public SessionDto SessionDto { get; init; }
        public ScoringDto ScoringDto { get; init; }
    }
}

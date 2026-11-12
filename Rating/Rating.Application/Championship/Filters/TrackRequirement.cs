namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;
    using DataModel.Snapshot;

    public class TrackRequirement : IChampionshipCondition
    {
        public string GetDescription(ChampionshipDto championshipDto)
        {
            if (championshipDto.ChampionshipState == ChampionshipState.Finished)
            {
                return string.Empty;
            }

            EventDto eventDto = championshipDto.Events[championshipDto.CurrentEventIndex];
            return eventDto.IsTrackNameExact ? $"Track has to be {eventDto.TrackName}" : $"Track should be {eventDto.TrackName}, but different track can be used.";
        }

        public ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet)
        {
            if (string.IsNullOrEmpty(dataSet.SessionInfo.TrackInfo?.TrackFullName))
            {
                return new ConditionResult(RequirementResultKind.DoesNotMatch, "No track selected.");
            }

            var currentEvent = championshipDto.GetCurrentOrLastEvent();
            var lastEventWithSession = championshipDto.GetLastSessionWithResults();

            if (!currentEvent.IsTrackNameExact && lastEventWithSession.EventDto != currentEvent)
            {
                return new ConditionResult(RequirementResultKind.PerfectMatch);
            }

            //If it is second session in an event, always return perfect match if sessions match
            if ((currentEvent.IsTrackNameExact || lastEventWithSession.EventDto == currentEvent) && currentEvent.TrackName == dataSet.SessionInfo.TrackInfo.TrackFullName)
            {
                return new ConditionResult(RequirementResultKind.PerfectMatch);
            }

            return new ConditionResult(RequirementResultKind.CanMatch);
        }
    }
}
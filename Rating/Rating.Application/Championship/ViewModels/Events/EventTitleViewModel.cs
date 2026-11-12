namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System.Text;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.ViewModels;

    public class EventTitleViewModel : AbstractViewModel<(ChampionshipDto Championship, EventDto EventDto, SessionDto SessionDto)>
    {
        public string ChampionshipName { get; set; }

        public string EventName { get; set; }

        public string EventIndex { get; set; }

        public string SessionName { get; set; }

        public string SessionIndex { get; set; }

        public string TrackName { get; set; }

        public string Remark { get; set; }

        protected override void ApplyModel((ChampionshipDto Championship, EventDto EventDto, SessionDto SessionDto) model)
        {
            (ChampionshipDto championship, EventDto eventDto, SessionDto sessionDto) = model;
            int eventIndex = championship.Events.IndexOf(eventDto);

            ChampionshipName = championship.ChampionshipName;
            EventName = eventDto.EventName;
            EventIndex = $"({eventIndex + 1} / {championship.Events.Count})";

            int sessionIndex = eventDto.Sessions.IndexOf(sessionDto);
            SessionName = sessionDto.Name;
            SessionIndex = $"({sessionIndex + 1} / {eventDto.Sessions.Count})";

            TrackName = eventDto.TrackName;

            SessionResultDto sessionResult = sessionDto.SessionResult;
            if (sessionResult == null || sessionResult.CompletionPercentage >= 1)
            {
                return;
            }

            StringBuilder remarkBuilder = new StringBuilder();
            if (championship.GetCurrentEvent() != eventDto)
            {
                remarkBuilder.Append("Red Flagged, ");
            }

            remarkBuilder.Append($"{(sessionResult.CompletionPercentage * 100):N0}% Completed, {sessionResult.Remarks}");
            if (remarkBuilder.Length > 200)
            {
                remarkBuilder.Remove(200, remarkBuilder.Length - 200);
                remarkBuilder.Append(" ...");
            }

            Remark = remarkBuilder.ToString();
        }

        public override (ChampionshipDto Championship, EventDto EventDto, SessionDto SessionDto) SaveToNewModel()
        {
            throw new System.NotSupportedException();
        }
    }
}
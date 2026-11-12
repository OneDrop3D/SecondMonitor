namespace SecondMonitor.Rating.Common.Championship
{
    using System.Linq;
    using DataModel.Championship.Events;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Calendar;

    public class CalendarDtoFactory
    {
        public CalendarDto Create(ChampionshipDto championshipDto)
        {
            CalendarDto calendarDto = new CalendarDto()
            {
                Name = championshipDto.ChampionshipName,
                Simulator = championshipDto.SimulatorName,
                Events = championshipDto.Events.Select(x => new CalendarEventDto()
                {
                    EventName = x.EventName,
                    TrackName = x.TrackName,
                    IsMysteryTrack = x.IsMysteryTrack,
                    EventDate = x.EventDate,
                    IsTrackNameExact = x.IsTrackNameExact || x.EventStatus == EventStatus.Finished,
                }).ToList()
            };

            if (championshipDto.Events.All(x => x.IsEventDateDefined))
            {
                calendarDto.IsChampionshipDatesFilled = true;
                calendarDto.StartDate = championshipDto.Events[0].EventDate;
                calendarDto.EndDate = championshipDto.Events[championshipDto.Events.Count - 1].EventDate;
            }

            return calendarDto;
        }
    }
}
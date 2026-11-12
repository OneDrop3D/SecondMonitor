namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;
    using Operations;

    using SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Session.SessionLength;
    using SecondMonitor.ViewModels.SimulatorContent;
    using ViewModels.Creation;
    using ViewModels.Creation.Calendar;
    using ViewModels.Creation.Session;

    public class ChampionshipFactory : IChampionshipFactory
    {
        private readonly ISimulatorContentProvider _simulatorContentProvider;
        private readonly ChampionshipManipulator _championshipManipulator;

        public ChampionshipFactory(ISimulatorContentProvider simulatorContentProvider, ChampionshipManipulator championshipManipulator)
        {
            _simulatorContentProvider = simulatorContentProvider;
            _championshipManipulator = championshipManipulator;
        }

        public ChampionshipDto Create(ChampionshipCreationViewModel championshipCreationViewModel)
        {
            ChampionshipDto championship = new()
            {
                ChampionshipState = ChampionshipState.NotStarted,
                SimulatorName = championshipCreationViewModel.SelectedSimulator,
                ChampionshipName = championshipCreationViewModel.ChampionshipTitle,
                AiNamesCanChange = championshipCreationViewModel.AiNamesCanChange,
                IsMysteryClass = championshipCreationViewModel.IsMysteryClass && championshipCreationViewModel.IsClassChangeEnabled,
                IsClassChangeEnabled = championshipCreationViewModel.IsClassChangeEnabled,
                IsManufacturerScoringEnabled = championshipCreationViewModel.IsManufacturerScoringEnabled,
                IsTeamScoringEnabled = championshipCreationViewModel.IsTeamScoringEnabled && championshipCreationViewModel.IsTeamScoringCheckBoxVisible,
                TeamScoringKind = championshipCreationViewModel.SelectedTeamsScoring.Value,
                ManufacturerScoringKind = championshipCreationViewModel.SelectedManufacturerScoring.Value,
                IsTrackAllClassesEnabled = championshipCreationViewModel.IsTrackAllClassesEnabled,
            };

            FillEvents(championship, championshipCreationViewModel);
            FillScoring(championship, championshipCreationViewModel);
            championship.NextTrack = championship.Events[0].TrackName;
            championship.TotalEvents = championship.Events.Count * championship.Events[0].Sessions.Count;
            championship.AllowedClasses = championshipCreationViewModel.ClassesSelectionViewModel.SaveToNewModel().ToList(); 

            if (championship.IsMysteryClass)
            {
                _championshipManipulator.ReRollMysteryProperties(championship);
            }

            return championship;
        }

        private void FillEvents(ChampionshipDto championshipDto, ChampionshipCreationViewModel championshipCreationViewModel)
        {
            List<SessionDefinitionViewModel> sessionDefinitionViewModels = championshipCreationViewModel.SessionsDefinitionViewModel.SessionsDefinitions.ToList();
            championshipDto.Events = championshipCreationViewModel.CalendarDefinitionViewModel.CalendarViewModel.CalendarEntries.Select(x => CreateEvent(x, sessionDefinitionViewModels)).ToList();
            var firstEvent = championshipDto.Events[0];
            if (firstEvent.IsMysteryTrack)
            {
                firstEvent.TrackName = _simulatorContentProvider.GetRandomTrack(championshipDto.SimulatorName)?.Name ?? string.Empty;
            }
        }

        public void FillScoring(ChampionshipDto championshipDto, ChampionshipCreationViewModel championshipCreationViewModel)
        {
            championshipDto.Scoring = championshipCreationViewModel.SessionsDefinitionViewModel.SessionsDefinitions.Select(x => new ScoringDto()
            {
                Scoring = x.Points.OrderBy(p => p.Position).Select(x => x.Points).ToList(),
                MaxPositionForFastestLap = x.MaxPositionForFastestLap,
                PointsForFastestLap = x.PointsForFastestLap,
            }).ToList();
        }

        private static EventDto CreateEvent(AbstractCalendarEntryViewModel calendarEntry, List<SessionDefinitionViewModel> sessionDefinitions)
        {
            EventDto newEventDto = new EventDto
            {
                EventName = calendarEntry.CustomEventName,
                TrackName = calendarEntry.TrackName,
                IsMysteryTrack = calendarEntry is MysteryCalendarEntryViewModel,
                IsTrackNameExact = calendarEntry is ExistingTrackCalendarEntryViewModel,
                Sessions = sessionDefinitions.Select(x => new SessionDto()
                    {
                        DistanceDescription = x.SelectedSessionLengthDefinitionViewModel.GetTextualDescription(calendarEntry.LayoutLength), 
                        Name = x.CustomSessionName,
                        IsDistanceBased = x.SelectedSessionLengthDefinitionViewModel.LengthKind == DistanceLengthDefinitionViewModel.DistanceLengthKind,
                        DistanceInMeters = x.SelectedSessionLengthDefinitionViewModel is DistanceLengthDefinitionViewModel distanceLengthDefinitionViewModel
                            ? distanceLengthDefinitionViewModel.GetDistance().InMeters : 0,
                    }).ToList(),
                IsEventDateDefined = calendarEntry.DateTime.HasValue,
                EventDate = calendarEntry.DateTime ?? DateTime.MinValue,
            };

            return newEventDto;
        }
    }
}
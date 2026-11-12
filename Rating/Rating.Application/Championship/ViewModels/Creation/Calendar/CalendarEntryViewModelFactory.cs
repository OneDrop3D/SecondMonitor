namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.Championship.Calendar;
    using Common.DataModel.Championship.Calendar;
    using Contracts.TrackMap;
    using Controller;
    using DataModel.SimulatorContent;
    using DataModel.TrackMap;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SimulatorContent;

    public class CalendarEntryViewModelFactory : ICalendarEntryViewModelFactory
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ITrackTemplateToSimTrackMapper _trackTemplateToSimTrackMapper;
        private readonly ISimulatorContentProvider _simulatorContentProvider;
        private readonly IMapsLoader _mapsLoader;

        public CalendarEntryViewModelFactory(IViewModelFactory viewModelFactory, ITrackTemplateToSimTrackMapper trackTemplateToSimTrackMapper, IMapsLoaderFactory mapsLoaderFactory, ISimulatorContentProvider simulatorContentProvider)
        {
            _viewModelFactory = viewModelFactory;
            _trackTemplateToSimTrackMapper = trackTemplateToSimTrackMapper;
            _simulatorContentProvider = simulatorContentProvider;
            _mapsLoader = mapsLoaderFactory.Create();
        }

        public AbstractCalendarEntryViewModel Create(AbstractTrackTemplateViewModel trackTemplate)
        {
            if (trackTemplate is ExistingTrackTemplateViewModel existingTrackTemplateViewModel)
            {
                var newEntry = _viewModelFactory.Create<ExistingTrackCalendarEntryViewModel>();
                newEntry.TrackName = existingTrackTemplateViewModel.TrackName;
                if (existingTrackTemplateViewModel.TrackGeometryViewModel.OriginalModel != null)
                {
                    newEntry.TrackGeometryViewModel.FromModel(existingTrackTemplateViewModel.TrackGeometryViewModel.OriginalModel);
                }

                newEntry.LayoutLength = existingTrackTemplateViewModel.LayoutLengthMeters;
                return newEntry;
            }

            if (trackTemplate is MysteryTrackTemplateViewModel mysteryTrackTemplateViewModel)
            {
                return new MysteryCalendarEntryViewModel();
            }

            return new EditableCalendarEntryViewModel()
            {
                TrackName = "ENTER TRACK NAME"
            };
        }

        public AbstractCalendarEntryViewModel Create(EventTemplate eventTemplate, string simulatorName, bool useCalendarEventNames, bool autoReplaceKnownTracks)
        {
            if (!string.IsNullOrEmpty(simulatorName) && autoReplaceKnownTracks && _trackTemplateToSimTrackMapper.TryGetSimulatorTrackName(simulatorName, eventTemplate.TrackTemplate.TrackName, out string simulatorTrackName))
            {
                Track trackDefinition = _simulatorContentProvider.GetAllTracksForSimulator(simulatorName).FirstOrDefault(x => x.Name == simulatorTrackName);
                if (trackDefinition != null)
                {
                    var newEntry = _viewModelFactory.Create<ExistingTrackCalendarEntryViewModel>();
                    newEntry.CustomEventName = useCalendarEventNames ? eventTemplate.EventName : string.Empty;
                    newEntry.TrackName = simulatorTrackName;
                    newEntry.LayoutLength = trackDefinition.LapDistance;
                    newEntry.DateTime = eventTemplate.EventDate;
                    bool hasMap = _mapsLoader.TryLoadMap(simulatorName, simulatorTrackName, out TrackMapDto trackMapDto);
                    if (hasMap)
                    {
                        newEntry.TrackGeometryViewModel.FromModel(trackMapDto.TrackGeometry);
                    }

                    return newEntry;
                }
            }

            return new CalendarPlaceholderEntryViewModel()
            {
                CustomEventName = useCalendarEventNames ? eventTemplate.EventName : string.Empty,
                LayoutLength = eventTemplate.TrackTemplate.LayoutLength,
                TrackName = eventTemplate.TrackTemplate.TrackName,
                DateTime = eventTemplate.EventDate,
            };
        }

        public IEnumerable<AbstractCalendarEntryViewModel> Create(CalendarDto calendarDto, string simulatorName)
        {
            bool isCalendarFromCurrentSim = calendarDto.Simulator == simulatorName;
            foreach (CalendarEventDto calendarDtoEvent in calendarDto.Events)
            {
                if (calendarDtoEvent.IsMysteryTrack)
                {
                    yield return new MysteryCalendarEntryViewModel()
                    {
                        DateTime = calendarDtoEvent.EventDate,
                    };
                }

                if (isCalendarFromCurrentSim && calendarDtoEvent.IsTrackNameExact)
                {
                    Track trackDefinition = _simulatorContentProvider.GetAllTracksForSimulator(simulatorName).FirstOrDefault(x => x.Name == calendarDtoEvent.TrackName);
                    if (trackDefinition != null)
                    {
                        var newEntry = _viewModelFactory.Create<ExistingTrackCalendarEntryViewModel>();
                        newEntry.CustomEventName = calendarDtoEvent.EventName;
                        newEntry.TrackName = calendarDtoEvent.TrackName;
                        newEntry.LayoutLength = trackDefinition.LapDistance;
                        newEntry.DateTime = calendarDtoEvent.EventDate;
                        bool hasMap = _mapsLoader.TryLoadMap(simulatorName, calendarDtoEvent.TrackName, out TrackMapDto trackMapDto);
                        if (hasMap)
                        {
                            newEntry.TrackGeometryViewModel.FromModel(trackMapDto.TrackGeometry);
                        }

                        yield return newEntry;
                        continue;
                    }
                }

                yield return new EditableCalendarEntryViewModel()
                {
                    CustomEventName = calendarDtoEvent.EventName,
                    TrackName = calendarDtoEvent.TrackName,
                    DateTime = calendarDtoEvent.EventDate,
                };
            }
        }
    }
}
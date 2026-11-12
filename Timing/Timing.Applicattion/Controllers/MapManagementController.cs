namespace SecondMonitor.Timing.Application.Controllers
{
    using System;
    using Common.SessionTiming.Drivers.Lap;
    using Contracts.TrackMap;
    using DataModel.TrackMap;
    using NLog;
    using SimdataManagement;

    public class MapManagementController : IMapManagementController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IMapsLoader _mapsLoader;
        private readonly ITrackDtoManipulator _trackDtoManipulator;
        private string _lastUnknownMap;

        public MapManagementController(TrackMapFromTelemetryFactory trackMapFromTelemetryFactory, IMapsLoaderFactory mapsLoaderFactory, ITrackDtoManipulator trackDtoManipulator)
        {
            _mapsLoader = mapsLoaderFactory.Create();
            _trackDtoManipulator = trackDtoManipulator;
            TrackMapFromTelemetryFactory = trackMapFromTelemetryFactory;
        }

        public event EventHandler<MapEventArgs> NewMapAvailable;
        public event EventHandler<MapEventArgs> MapRemoved;

        public TrackMapFromTelemetryFactory TrackMapFromTelemetryFactory { get; }

        public TimeSpan MapPointsInterval
        {
            get => TrackMapFromTelemetryFactory.MapsPointsInterval;
            set => TrackMapFromTelemetryFactory.MapsPointsInterval = value;
        }

        public void OnLapCompleted(LapEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastUnknownMap) || !e.Lap.Driver.IsPlayer || !e.Lap.Valid || e.Lap.LapTelemetryInfo?.TimedTelemetrySnapshots == null || e.Lap.LapTelemetryInfo.TimedTelemetrySnapshots.Snapshots.Count <= 0)
            {
                return;
            }

            if (e.Lap.Driver.Session.LastSet.SimulatorSourceInfo.WorldPositionInvalid)
            {
                return;
            }

            string formattedTrackName = e.Lap.Driver.Session.LastSet.SessionInfo.TrackInfo.TrackFullName;

            if (formattedTrackName != _lastUnknownMap)
            {
                return;
            }

            TrackGeometryDto newTrackGeometryDto = TrackMapFromTelemetryFactory.BuildTrackGeometryDto(e.Lap.LapTelemetryInfo.TimedTelemetrySnapshots);
            TrackMapDto newTrack = new TrackMapDto()
            {
                TrackName = e.Lap.Driver.Session.LastSet.SessionInfo.TrackInfo.TrackName,
                LayoutName = e.Lap.Driver.Session.LastSet.SessionInfo.TrackInfo.TrackLayoutName,
                TrackGeometry = newTrackGeometryDto,
                SimulatorSource = e.Lap.Driver.Session.LastSet.Source,
            };

            Logger.Info($"Notified on Lap Completed on unknown map: {formattedTrackName}, Saving");
            SaveMap(e.Lap.Driver.Session.LastSet.Source, formattedTrackName, newTrack);
        }

        public bool TryGetMap(string simulator, string fullTrackName, out TrackMapDto trackMapDto)
        {
            if (!_mapsLoader.TryLoadMap(simulator, fullTrackName, out trackMapDto))
            {
                Logger.Info($"Trying to get map: {fullTrackName}, Map unknown");
                _lastUnknownMap = fullTrackName;
                return false;
            }

            if (trackMapDto.TrackGeometry.ExporterVersion == TrackMapFromTelemetryFactory.ExporterVersion)
            {
                Logger.Info($"Trying to get map: {fullTrackName}, Map Loaded.");
                return true;
            }

            Logger.Info($"Trying to get map: {fullTrackName}, Map Loaded, but outdated version.");
            _lastUnknownMap = fullTrackName;
            return false;
        }

        public void RemoveMap(string simulator, string trackFullName)
        {
            if (!TryGetMap(simulator, trackFullName, out TrackMapDto trackMapDto))
            {
                return;
            }

            Logger.Info($"Trying to remove map: Simulator {simulator}, Track Name '{trackFullName}'");
            _mapsLoader.RemoveMap(simulator, trackFullName);
            MapRemoved?.Invoke(this, new MapEventArgs(trackMapDto));
        }

        public TrackMapDto RotateMapRight(string simulator, string trackFullName)
        {
            if (!TryGetMap(simulator, trackFullName, out TrackMapDto trackMapDto))
            {
                throw new ArgumentException($"Unknown Map: {simulator} - {trackFullName}");
            }

            trackMapDto = _trackDtoManipulator.RotateRight(trackMapDto);
            Logger.Info($"Trying to rotate map right: Simulator {simulator}, Track Name '{trackFullName}'");
            SaveMap(simulator, trackFullName, trackMapDto);
            return trackMapDto;
        }

        public TrackMapDto RotateMapLeft(string simulator, string trackFullName)
        {
            if (!TryGetMap(simulator, trackFullName, out TrackMapDto trackMapDto))
            {
                throw new ArgumentException($"Unknown Map: {simulator} - {trackFullName}");
            }

            trackMapDto = _trackDtoManipulator.RotateLeft(trackMapDto);
            Logger.Info($"Trying to rotate left: Simulator {simulator}, Track Name '{trackFullName}'");
            SaveMap(simulator, trackFullName, trackMapDto);
            return trackMapDto;
        }

        private void SaveMap(string simulator, string formattedTrackName, TrackMapDto trackMapDto)
        {
            Logger.Info($"Trying to save map: Simulator {simulator}, Track Name '{formattedTrackName}'");
            _mapsLoader.SaveMap(simulator, formattedTrackName, trackMapDto);
            _lastUnknownMap = string.Empty;
            NewMapAvailable?.Invoke(this, new MapEventArgs(trackMapDto));
        }
    }
}
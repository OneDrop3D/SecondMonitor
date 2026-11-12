namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.MapView
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contracts.TrackMap;
    using DataModel.TrackMap;
    using NLog;

    using Synchronization;
    using TelemetryManagement.DTO;
    using ViewModels.MapView;

    public class MapViewController : IMapViewController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly IMapsLoader _mapsLoader;
        private readonly Dictionary<string, MapViewDriverInfoFacade> _fakeDrivers;

        private TrackMapDto _lastMap;
        private bool _mapAvailable;

        public MapViewController(IMapsLoaderFactory mapsLoaderFactory, ITelemetryViewsSynchronization telemetryViewsSynchronization)
        {
            _fakeDrivers = new Dictionary<string, MapViewDriverInfoFacade>();
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _mapsLoader = mapsLoaderFactory.Create();
        }

        public IMapViewViewModel MapViewViewModel { get; set; }

        public Task StartControllerAsync()
        {
            Subscribe();
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            UnSubscribe();
            return Task.CompletedTask;
        }

        private static string FormatTrackName(string trackName, string layoutName)
        {
            return string.IsNullOrEmpty(layoutName) ? trackName : $"{trackName}-{layoutName}";
        }

        private void InitializeViewModel(SessionInfoDto sessionInfo)
        {
            _fakeDrivers.Clear();
            string formattedTrackName = FormatTrackName(sessionInfo.TrackName, sessionInfo.LayoutName);
            _mapAvailable = _mapsLoader.TryLoadMap(sessionInfo.Simulator, formattedTrackName, out _lastMap);
            if (_mapAvailable)
            {
                MapViewViewModel.LoadTrack(_lastMap);
            }
        }

        private void Subscribe()
        {
            _telemetryViewsSynchronization.NewSessionLoaded += TelemetryViewsSynchronizationOnNewSessionLoaded;
            _telemetryViewsSynchronization.LapLoaded += TelemetryViewsSynchronization_LapLoaded;
            _telemetryViewsSynchronization.LapUnloaded += TelemetryViewsSynchronizationOnLapUnloaded;
            _telemetryViewsSynchronization.SyncTelemetryView += TelemetryViewsSynchronizationOnSyncTelemetryView;
        }

        private void UnSubscribe()
        {
            _telemetryViewsSynchronization.NewSessionLoaded -= TelemetryViewsSynchronizationOnNewSessionLoaded;
            _telemetryViewsSynchronization.LapLoaded -= TelemetryViewsSynchronization_LapLoaded;
            _telemetryViewsSynchronization.LapUnloaded -= TelemetryViewsSynchronizationOnLapUnloaded;
            _telemetryViewsSynchronization.SyncTelemetryView -= TelemetryViewsSynchronizationOnSyncTelemetryView;
        }

        private async void TelemetryViewsSynchronization_LapLoaded(object sender, LapsTelemetryArgs lapsTelemetryArgs)
        {
            if (!_mapAvailable)
            {
                return;
            }

            try
            {
                foreach (LapTelemetryDto lapTelemetryDto in lapsTelemetryArgs.LapsTelemetries)
                {
                    await MapViewViewModel.AddPathsForLap(lapTelemetryDto);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading lap telemetry");
            }
        }

        private void TelemetryViewsSynchronizationOnLapUnloaded(object sender, LapsSummaryArgs lapsSummaryArgs)
        {
            if (!_mapAvailable)
            {
                return;
            }

            foreach (LapSummaryDto lapSummaryDto in lapsSummaryArgs.LapsSummaries)
            {
                MapViewViewModel.RemovePathsForLap(lapSummaryDto);

                if (_fakeDrivers.TryGetValue(lapSummaryDto.Id, out MapViewDriverInfoFacade fakeDriver))
                {
                    MapViewViewModel.RemoveDriver(fakeDriver);
                }
            }
        }

        private void TelemetryViewsSynchronizationOnSyncTelemetryView(object sender, TelemetrySnapshotArgs e)
        {
            if (!_mapAvailable)
            {
                return;
            }

            string driverId = e.LapSummaryDto.Id;
            if (!_fakeDrivers.TryGetValue(driverId, out MapViewDriverInfoFacade fakeDriver))
            {
                fakeDriver = new MapViewDriverInfoFacade(e.TelemetrySnapshot.PlayerData, e.LapSummaryDto.LapNumber, driverId);
                _fakeDrivers.Add(driverId, fakeDriver);
                MapViewViewModel.AddDriver(fakeDriver);
            }
            else
            {
                fakeDriver.ParentInfo = e.TelemetrySnapshot.PlayerData;
            }

            MapViewViewModel.UpdateDriver(fakeDriver);
        }

        private async void TelemetryViewsSynchronizationOnNewSessionLoaded(object sender, TelemetrySessionArgs e)
        {
            await Task.Run(() => InitializeViewModel(e.SessionInfoDto));
        }
    }
}
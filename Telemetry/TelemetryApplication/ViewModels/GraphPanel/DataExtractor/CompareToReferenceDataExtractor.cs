namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DataExtractor
{
    using System;
    using System.Linq;
    using Controllers.Synchronization;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using LoadedLapCache;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;
    using TelemetryManagement.StoryBoard;

    public class CompareToReferenceDataExtractor : ISingleSeriesDataExtractor
    {
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly TelemetryStoryBoardFactory _telemetryStoryBoardFactory;
        private readonly ILoadedLapsCache _loadedLapsCache;
        private TelemetryStoryboard _referenceLap;

        public CompareToReferenceDataExtractor(ITelemetryViewsSynchronization telemetryViewsSynchronization, TelemetryStoryBoardFactory telemetryStoryBoardFactory, ILoadedLapsCache loadedLapsCache)
        {
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _telemetryStoryBoardFactory = telemetryStoryBoardFactory;
            _loadedLapsCache = loadedLapsCache;
            Subscribe();
            InitializeAlreadyLoadedLaps();
        }

        public event EventHandler<EventArgs> DataRefreshRequested;

        public string ExtractorName => "Δ to Reference";

        private void InitializeAlreadyLoadedLaps()
        {
            ChangeReferenceLap(_loadedLapsCache.ReferenceLap);
        }

        private void Subscribe()
        {
            _telemetryViewsSynchronization.ReferenceLapSelected += TelemetryViewsSynchronizationOnReferenceLapSelected;
        }

        private void UnSubscribe()
        {
            _telemetryViewsSynchronization.ReferenceLapSelected -= TelemetryViewsSynchronizationOnReferenceLapSelected;
        }

        private void TelemetryViewsSynchronizationOnReferenceLapSelected(object sender, LapSummaryArgs e)
        {
            ChangeReferenceLap(e.LapSummary);
        }

        private void ChangeReferenceLap(LapSummaryDto lapSummaryDto)
        {
            if (lapSummaryDto == null)
            {
                _referenceLap = null;
                DataRefreshRequested?.Invoke(this, new EventArgs());
                return;
            }

            var referenceLap = _loadedLapsCache.LoadedLaps.FirstOrDefault(x => x.LapSummary.Id == lapSummaryDto.Id);
            if (referenceLap == null)
            {
                return;
            }

            _referenceLap = _telemetryStoryBoardFactory.Create(referenceLap);

            DataRefreshRequested?.Invoke(this, new EventArgs());
        }

        public double GetValue(Func<TimedTelemetrySnapshot, CarPropertiesDto, double> valueExtractFunction, TimedTelemetrySnapshot telemetrySnapshot, CarPropertiesDto carPropertiesDto, XAxisKind xAxisKind)
        {
            if (_referenceLap == null)
            {
                return valueExtractFunction(telemetrySnapshot, carPropertiesDto);
            }

            double toCompareValue = xAxisKind == XAxisKind.LapDistance ?
                _referenceLap.GetValueByDistance(Distance.FromMeters(telemetrySnapshot.PlayerData.LapDistance), carPropertiesDto, valueExtractFunction) :
                _referenceLap.GetValueByTime(telemetrySnapshot.LapTime, carPropertiesDto, valueExtractFunction);

            return valueExtractFunction(telemetrySnapshot, carPropertiesDto) - toCompareValue;
        }

        public bool ShowLapGraph(LapSummaryDto lapSummaryDto)
        {
            return lapSummaryDto.Id != _referenceLap?.LapSummaryDto.Id;
        }

        public void Dispose()
        {
            UnSubscribe();
        }
    }
}
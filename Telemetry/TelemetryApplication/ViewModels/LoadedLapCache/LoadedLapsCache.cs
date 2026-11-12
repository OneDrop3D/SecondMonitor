namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LoadedLapCache
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Synchronization;
    using TelemetryManagement.DTO;

    public class LoadedLapsCache : ILoadedLapsCache
    {
        private readonly object _lockObject = new object();
        private readonly List<LapTelemetryDto> _loadedLaps;

        public LoadedLapsCache(ITelemetryViewsSynchronization telemetryViewsSynchronization)
        {
            _loadedLaps = new List<LapTelemetryDto>();
            telemetryViewsSynchronization.LapLoaded += TelemetryViewsSynchronizationOnLapLoaded;
            telemetryViewsSynchronization.LapUnloaded += TelemetryViewsSynchronizationOnLapUnloaded;
            telemetryViewsSynchronization.ReferenceLapSelected += TelemetryViewsSynchronizationOnReferenceLapSelected;
        }

        public IReadOnlyCollection<LapTelemetryDto> LoadedLaps
        {
            get
            {
                lock (_lockObject)
                {
                    return _loadedLaps.AsReadOnly();
                }
            }
        }

        public LapSummaryDto ReferenceLap { get; set; }

        public bool TryGetLapTelemetry(LapSummaryDto lapSummaryDto, out LapTelemetryDto lapTelemetryDto)
        {
            lock (_lockObject)
            {
                lapTelemetryDto = _loadedLaps.FirstOrDefault(x => x.LapSummary == lapSummaryDto);
            }

            return lapTelemetryDto != null;
        }

        private void TelemetryViewsSynchronizationOnReferenceLapSelected(object sender, LapSummaryArgs e)
        {
            ReferenceLap = e.LapSummary;
        }

        private void TelemetryViewsSynchronizationOnLapUnloaded(object sender, LapsSummaryArgs lapsSummaryArgs)
        {
            lock (_lockObject)
            {
                _loadedLaps.RemoveAll(x => lapsSummaryArgs.LapsSummaries.Contains(x.LapSummary));
            }
        }

        private void TelemetryViewsSynchronizationOnLapLoaded(object sender, LapsTelemetryArgs lapsTelemetryArgs)
        {
            lock (_lockObject)
            {
                _loadedLaps.AddRange(lapsTelemetryArgs.LapsTelemetries);
            }
        }
    }
}
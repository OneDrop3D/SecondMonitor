namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;

    using TelemetryManagement.DTO;
    using ViewModels.LoadedLapCache;

    public class LapStintSynchronization : ILapStintSynchronization
    {
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly Dictionary<string, int> _lapToStintMap;
        public LapStintSynchronization(ITelemetryViewsSynchronization telemetryViewsSynchronization, ILoadedLapsCache loadedLapsCache)
        {
            telemetryViewsSynchronization.LapLoaded += TelemetryViewsSynchronizationOnLapLoaded;
            _loadedLapsCache = loadedLapsCache;
            _lapToStintMap = new Dictionary<string, int>();
        }

        public event EventHandler<LapStintArg> LapStintChanged;

        private void TelemetryViewsSynchronizationOnLapLoaded(object sender, LapsTelemetryArgs lapsTelemetryArgs)
        {
            foreach (LapTelemetryDto lapTelemetryDto in lapsTelemetryArgs.LapsTelemetries)
            {
                if (_lapToStintMap.TryGetValue(lapTelemetryDto.LapSummary.Id, out int stintNumber))
                {
                    lapTelemetryDto.LapSummary.Stint = stintNumber;
                }
            }
        }

        public void SetStintNumberForLap(string lapId, int stint)
        {
            _lapToStintMap[lapId] = stint;
            _loadedLapsCache.LoadedLaps.Where(x => x.LapSummary.Id == lapId).ForEach(x => x.LapSummary.Stint = stint);
            LapStintChanged?.Invoke(this, new LapStintArg(lapId, stint));
        }
    }
}
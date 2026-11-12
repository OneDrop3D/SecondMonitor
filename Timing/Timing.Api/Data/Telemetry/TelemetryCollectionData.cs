namespace SecondMonitor.Timing.Api.Data.Telemetry
{
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Telemetry;

    public class TelemetryCollectionData
    {
        public TelemetryCollectionData(SessionIdentification sessionIdentification, string driverId, IEnumerable<TimedTelemetrySnapshot> timedTelemetryData)
        {
            SessionIdentification = sessionIdentification;
            DriverId = driverId;
            List<TimedTelemetrySnapshot> timedTelemetrySnapshots = timedTelemetryData.ToList();
            TimedTelemetryData = timedTelemetrySnapshots.Select(x => new TimedTelemetryData(x)).ToList();
            TelemetryInfo = timedTelemetrySnapshots.First().SimulatorSourceInfo.TelemetryInfo;
        }

        public SessionIdentification SessionIdentification { get; }

        public string DriverId { get; }

        public IReadOnlyList<TimedTelemetryData> TimedTelemetryData { get; }

        public TelemetryInfo TelemetryInfo { get; }
    }
}

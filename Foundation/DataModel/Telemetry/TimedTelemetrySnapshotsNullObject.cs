namespace SecondMonitor.DataModel.Telemetry
{
    using System;
    using System.Collections.Generic;
    using BasicProperties;
    using Snapshot;
    using Snapshot.Drivers;

    public class TimedTelemetrySnapshotsNullObject : ITimedTelemetrySnapshots
    {
        public TimedTelemetrySnapshotsNullObject()
        {
            Snapshots = new List<TimedTelemetrySnapshot>();
        }

        public IReadOnlyCollection<TimedTelemetrySnapshot> Snapshots { get; }

        public void AddNextSnapshot(TimeSpan lapTime, TimeSpan sessionTIme, DriverInfo driverInfo, WeatherInfo weatherInfo, InputInfo inputInfo, SimulatorSourceInfo simulatorSource)
        {
        }

        public void TrimInvalid(Distance lapDistance)
        {
        }
    }
}
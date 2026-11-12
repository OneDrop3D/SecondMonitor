namespace SecondMonitor.DataModel.Telemetry
{
    using System;
    using System.Collections.Generic;
    using BasicProperties;
    using Snapshot;
    using Snapshot.Drivers;

    public interface ITimedTelemetrySnapshots
    {
         IReadOnlyCollection<TimedTelemetrySnapshot> Snapshots { get; }

         void AddNextSnapshot(TimeSpan lapTime, TimeSpan sessionTime, DriverInfo driverInfo, WeatherInfo weatherInfo, InputInfo inputInfo, SimulatorSourceInfo simulatorSource);
         void TrimInvalid(Distance lapDistance);
    }
}
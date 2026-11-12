namespace SecondMonitor.DataModel.Telemetry
{
    using System;
    using System.Diagnostics;
    using System.Xml.Serialization;
    using BasicProperties;
    using ProtoBuf;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    [Serializable]
    [ProtoContract]
    [DebuggerDisplay("Lap time: {LapTime}")]
    public class TimedTelemetrySnapshot : TelemetrySnapshot
    {
        public TimedTelemetrySnapshot()
        {
        }

        public TimedTelemetrySnapshot(TimeSpan lapTime, TimeSpan sessionTime, DriverInfo playerInfo, WeatherInfo weatherInfo, InputInfo inputInfo, SimulatorSourceInfo simulatorSourceInfo) : base(playerInfo, weatherInfo, inputInfo, simulatorSourceInfo)
        {
            LapTime = lapTime;
            SessionTime = sessionTime;
        }

        [XmlIgnore]
        public TimeSpan SessionTime { get; }

        [XmlIgnore]
        public TimeSpan LapTime { get; set; }

        [XmlAttribute]
        [ProtoMember(1, IsRequired = true)]
        public double LapTimeSeconds
        {
            get => LapTime.TotalSeconds;
            set => LapTime = TimeSpan.FromSeconds(value);
        }

        public void Accept(ITimedTelemetrySnapshotVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
namespace SecondMonitor.DataModel.Summary
{
    using System;
    using System.Xml.Serialization;
    using BasicProperties;
    using Newtonsoft.Json;
    using Telemetry;

    [Serializable]
    public sealed class Lap
    {
        public Lap()
        {
        }

        public Lap(Driver driver, bool isValid)
        {
            Driver = driver;
            IsValid = isValid;
        }

        [XmlIgnore]
        [JsonIgnore]
        public Driver Driver { get; set; }

        public bool IsPitLap { get; set; }

        public bool IsPitEntryLap { get; set; }

        public bool IsPitExitLap { get; set; }

        public int LapNumber { get; set; }

        public bool IsValid { get; set; }

        public int LapStartPosition => LapStartSnapshot?.PlayerData?.Position ?? 0;

        public int LapStartPositionClass => LapStartSnapshot?.PlayerData?.PositionInClass ?? 0;

        [JsonIgnore]
        [XmlIgnore]
        public SessionType SessionType { get; set; }

        public TimeSpan LapTime { get; set; } = TimeSpan.Zero;

        public TimeSpan Sector1 { get; set; } = TimeSpan.Zero;

        public TimeSpan Sector2 { get; set; } = TimeSpan.Zero;

        public TimeSpan Sector3 { get; set; } = TimeSpan.Zero;

        [JsonIgnore]
        public TelemetrySnapshot LapEndSnapshot { get; set; }

        [JsonIgnore]
        public TelemetrySnapshot LapStartSnapshot { get; set; }
    }
}
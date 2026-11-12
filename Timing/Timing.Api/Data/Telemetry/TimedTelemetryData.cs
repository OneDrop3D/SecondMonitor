namespace SecondMonitor.Timing.Api.Data.Telemetry
{
    using SecondMonitor.DataModel.Telemetry;

    public class TimedTelemetryData
    {
        public TimedTelemetryData(TimedTelemetrySnapshot timedTelemetrySnapshot)
        {
            LapTimeSeconds = timedTelemetrySnapshot.LapTimeSeconds;
            InputData = new InputData(timedTelemetrySnapshot.InputInfo);
            WeatherInfoData = new WeatherInfoData(timedTelemetrySnapshot.WeatherInfo);
            CarData = new CarData(timedTelemetrySnapshot.PlayerData.CarInfo);
            CompletedLaps = timedTelemetrySnapshot.PlayerData.CompletedLaps;
            InPits = timedTelemetrySnapshot.PlayerData.InPits;
            Position = timedTelemetrySnapshot.PlayerData.Position;
            PositionInClass = timedTelemetrySnapshot.PlayerData.PositionInClass;
            LapDistance = timedTelemetrySnapshot.PlayerData.LapDistance;
            WorldPosition = new Point3DData(timedTelemetrySnapshot.PlayerData.WorldPosition);
            SpeedKph = timedTelemetrySnapshot.PlayerData.Speed.InKph;
        }

        public double LapTimeSeconds { get; }

        public InputData InputData { get; }

        public WeatherInfoData WeatherInfoData { get; }

        public CarData CarData { get; }

        public int CompletedLaps { get; }

        public bool InPits { get; }

        public int Position { get; }

        public int PositionInClass { get; }

        public double LapDistance { get; }

        public Point3DData WorldPosition { get; }

        public double SpeedKph { get; }
    }
}

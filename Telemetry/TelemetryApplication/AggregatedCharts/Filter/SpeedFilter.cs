namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Filter
{
    using DataModel.BasicProperties;
    using DataModel.Telemetry;

    public class SpeedFilter : ITelemetryFilter
    {
        public SpeedFilter()
        {
            MinimalSpeed = Velocity.Zero;
            MaximumSpeed = Velocity.FromKph(100000);
        }

        public Velocity MinimalSpeed { get; set; }

        public Velocity MaximumSpeed { get; set; }

        public bool Accepts(TimedTelemetrySnapshot dataSet)
        {
            return dataSet.PlayerData.Speed.InMs > MinimalSpeed.InMs && dataSet.PlayerData.Speed.InMs < MaximumSpeed.InMs;
        }
    }
}
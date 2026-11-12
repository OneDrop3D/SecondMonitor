namespace SecondMonitor.Telemetry.TelemetryManagement.Settings.ChartProperties
{
    using DataModel.BasicProperties;

    public class LatAccToSteeringChartProperties
    {
        public LatAccToSteeringChartProperties()
        {
            MinimumSpeed = Velocity.Zero;
            MaximumSpeed = Velocity.FromKph(500);
        }

        public Velocity MinimumSpeed { get; set; }
        public Velocity MaximumSpeed { get; set; }
    }
}
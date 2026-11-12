namespace SecondMonitor.Telemetry.TelemetryManagement.Settings.ChartProperties
{
    public class ChartsProperties
    {
        public ChartsProperties()
        {
            SuspensionVelocityHistogram = new SuspensionVelocityHistogram();
            CamberHistogram = new CamberHistogram();
            LatAccToSteeringChartProperties = new LatAccToSteeringChartProperties();
        }

        public SuspensionVelocityHistogram SuspensionVelocityHistogram { get; set; }

        public CamberHistogram CamberHistogram { get; set; }

        public LatAccToSteeringChartProperties LatAccToSteeringChartProperties { get; set; }
    }
}
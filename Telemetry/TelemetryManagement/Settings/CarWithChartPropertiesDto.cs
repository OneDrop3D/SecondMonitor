namespace SecondMonitor.Telemetry.TelemetryManagement.Settings
{
    using ChartProperties;

    public class CarWithChartPropertiesDto
    {
        public CarWithChartPropertiesDto()
        {
            ChartsProperties = new ChartsProperties();
            CarPropertiesDto = new CarPropertiesDto();
        }

        public ChartsProperties ChartsProperties { get; set; }

        public CarPropertiesDto CarPropertiesDto { get; set; }
    }
}
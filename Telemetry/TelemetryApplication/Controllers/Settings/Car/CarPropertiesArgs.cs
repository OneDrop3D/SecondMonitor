namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using System;
    using TelemetryManagement.Settings;

    public class CarPropertiesArgs : EventArgs
    {
        public CarPropertiesArgs(CarWithChartPropertiesDto carWithChartProperties)
        {
            CarWithChartProperties = carWithChartProperties;
        }

        public CarWithChartPropertiesDto CarWithChartProperties { get; }
    }
}
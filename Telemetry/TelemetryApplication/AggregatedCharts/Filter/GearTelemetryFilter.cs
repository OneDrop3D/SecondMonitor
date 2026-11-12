namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Filter
{
    using DataModel.Telemetry;

    public class GearTelemetryFilter : IGearTelemetryFilter
    {
        public string FilterGear { get; set; }

        public bool OnThrottle { get; set; }

        public bool Accepts(TimedTelemetrySnapshot dataSet)
        {
            var result = string.IsNullOrWhiteSpace(FilterGear) || dataSet.PlayerData.CarInfo.CurrentGear == FilterGear;

            if (OnThrottle)
            {
                result &= dataSet.InputInfo.ThrottlePedalPosition > 0.05;
            }

            return result;
        }
    }
}
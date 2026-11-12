namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class RpmToHorizontalGExtractor : AbstractGearFilteredScatterPlotExtractor
    {
        public RpmToHorizontalGExtractor(ISettingsProvider settingsProvider, IEnumerable<ITelemetryFilter> filters, IGearTelemetryFilter gearTelemetryFilter, ISettingsController settingsController)
            : base(settingsProvider, filters, gearTelemetryFilter, settingsController)
        {
        }

        public override string YUnit => "G";

        public override string XUnit => "RPM";

        public override double XMajorTickSize => 1000;

        public override double YMajorTickSize => 1;

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.EngineRpm;
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.ZinG;
        }
    }
}
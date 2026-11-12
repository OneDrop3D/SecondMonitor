namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using Controllers.Settings;
    using DataModel.Telemetry;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class LateralToLongGExtractor : AbstractScatterPlotExtractor
    {
        public LateralToLongGExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        public override string XUnit => "G";

        public override double XMajorTickSize => 0.5;

        public override string YUnit => "G";

        public override double YMajorTickSize => 0.5;

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.XinG;
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.ZinG;
        }
    }
}
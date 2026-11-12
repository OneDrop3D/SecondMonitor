namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using Controllers.Settings;

    using DataModel.BasicProperties;
    using DataModel.Telemetry;

    using SecondMonitor.ViewModels.Settings;

    using TelemetryManagement.Settings;

    public class LatAccelerationToFrontRollAngleExtractor : AbstractScatterPlotExtractor
    {
        public LatAccelerationToFrontRollAngleExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        public override string YUnit => Angle.GetUnitsSymbol(AngleUnits);
        public override string XUnit => Angle.GetUnitsSymbol(AngleUnits);
        public override double XMajorTickSize => 1;
        public override double YMajorTickSize => 0.5;

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.XinG;
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData?.CarInfo?.FrontRollAngle?.InDegrees ?? 0;
        }
    }
}

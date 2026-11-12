namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class RearRollAngleToFrontRollAngleExtractor : AbstractScatterPlotExtractor
    {
        public RearRollAngleToFrontRollAngleExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        public override string YUnit => Angle.GetUnitsSymbol(AngleUnits);
        public override string XUnit => Angle.GetUnitsSymbol(AngleUnits);
        public override double XMajorTickSize => Angle.GetFromDegrees(0.5).GetValueInUnits(AngleUnits);
        public override double YMajorTickSize => Angle.GetFromDegrees(0.5).GetValueInUnits(AngleUnits);

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData?.CarInfo?.RearRollAngle?.GetValueInUnits(AngleUnits) ?? 0;
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData?.CarInfo?.FrontRollAngle?.GetValueInUnits(AngleUnits) ?? 0;
        }
    }
}
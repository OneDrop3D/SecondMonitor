namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;
    using DataModel.Telemetry;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class RpmToPowerExtractor : AbstractScatterPlotExtractor
    {
        public RpmToPowerExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        public override string YUnit => Power.GetUnitSymbol(PowerUnits);
        public override string XUnit => "RPM";
        public override double XMajorTickSize => 1000;
        public override double YMajorTickSize => PowerUnits == PowerUnits.HP ? 100 : Math.Round(Power.FromKw(100).GetValueInUnit(PowerUnits), 0);
        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.EngineRpm;
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.EnginePower.GetValueInUnit(PowerUnits);
        }
    }
}
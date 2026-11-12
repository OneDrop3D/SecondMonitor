namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System;
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class TyreLoadToLatGExtractor : AbstractWheelScatterPlotDataExtractor
    {
        public TyreLoadToLatGExtractor(ISettingsProvider settingsProvider, IEnumerable<ITelemetryFilter> filters, ISettingsController settingsController) : base(settingsProvider, filters, settingsController)
        {
        }

        public override string YUnit => Force.GetUnitSymbol(ForceUnits);
        public override string XUnit => "G";
        public override double XMajorTickSize => 1;
        public override double YMajorTickSize => Math.Round(Force.GetFromNewtons(1000).GetValueInUnits(ForceUnits));
        protected override double GetXWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.XinG;
        }

        protected override double GetYWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return wheelInfo.TyreLoad.GetValueInUnits(ForceUnits);
        }
    }
}
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

    public class HorizontalAccelerationToRideHeightExtractor : AbstractWheelScatterPlotDataExtractor
    {
        public HorizontalAccelerationToRideHeightExtractor(ISettingsProvider settingsProvider, IEnumerable<ITelemetryFilter> filters, ISettingsController settingsController) : base(settingsProvider, filters, settingsController)
        {
        }

        public override string YUnit => Distance.GetUnitsSymbol(DistanceUnitsSmall);

        public override string XUnit => "G";

        public override double XMajorTickSize => 0.5;

        public override double YMajorTickSize => Math.Round(Distance.FromMeters(0.05).GetByUnit(DistanceUnitsSmall));

        protected override double GetXWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.ZinG;
        }

        protected override double GetYWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return wheelInfo.RideHeight.GetByUnit(DistanceUnitsSmall);
        }
    }
}
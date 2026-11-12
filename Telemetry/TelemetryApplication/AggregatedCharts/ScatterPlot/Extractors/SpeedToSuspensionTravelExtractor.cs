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

    public class SpeedToSuspensionTravelExtractor : AbstractWheelScatterPlotDataExtractor
    {
        public SpeedToSuspensionTravelExtractor(ISettingsProvider settingsProvider, IEnumerable<ITelemetryFilter> filters, ISettingsController settingsController) : base(settingsProvider, filters, settingsController)
        {
        }

        public override string YUnit => Distance.GetUnitsSymbol(DistanceUnitsSmall);

        public override string XUnit => Velocity.GetUnitSymbol(VelocityUnits);

        public override double XMajorTickSize => VelocityUnits == VelocityUnits.Mph ? Velocity.FromMph(50).GetValueInUnits(VelocityUnits) : Velocity.FromKph(50).GetValueInUnits(VelocityUnits);
        public override double YMajorTickSize => Math.Round(Distance.FromMeters(0.05).GetByUnit(DistanceUnitsSmall));

        protected override double GetXWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.Speed.GetValueInUnits(VelocityUnits);
        }

        protected override double GetYWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return wheelInfo.SuspensionTravel.GetByUnit(DistanceUnitsSmall);
        }
    }
}
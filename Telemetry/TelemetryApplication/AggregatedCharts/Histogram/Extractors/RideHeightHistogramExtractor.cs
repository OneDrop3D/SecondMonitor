namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Extractors
{
    using System;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class RideHeightHistogramExtractor : AbstractWheelHistogramDataExtractor
    {
        public RideHeightHistogramExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        protected override bool IsZeroBandInMiddle => true;
        public override double DefaultBandSize => Math.Round(Distance.FromMeters(0.005).GetByUnit(DistanceUnitsSmall), 2);
        public override string YUnit => Distance.GetUnitsSymbol(DistanceUnitsSmall);
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> WheelValueExtractor => (_, x, __) => x.RideHeight?.GetByUnit(DistanceUnitsSmall) ?? 0;
    }
}
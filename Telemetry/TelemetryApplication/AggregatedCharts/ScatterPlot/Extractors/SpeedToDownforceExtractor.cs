namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System;
    using Controllers.Settings;
    using DataAdapters;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class SpeedToDownforceExtractor : AbstractScatterPlotExtractor
    {
        private readonly DownforceAdapter _downforceAdapter;

        public SpeedToDownforceExtractor(ISettingsProvider settingsProvider, DownforceAdapter downforceAdapter, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
            _downforceAdapter = downforceAdapter;
        }

        public override string YUnit => Force.GetUnitSymbol(ForceUnits);

        public override string XUnit => Velocity.GetUnitSymbol(VelocityUnits);

        public override double XMajorTickSize => VelocityUnits == VelocityUnits.Mph ? Velocity.FromMph(50).GetValueInUnits(VelocityUnits) : Velocity.FromKph(50).GetValueInUnits(VelocityUnits);
        public override double YMajorTickSize => Math.Round(Force.GetFromNewtons(2500).GetValueInUnits(ForceUnits));

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.Speed.GetValueInUnits(VelocityUnits);
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            IsUsingComputedData = _downforceAdapter.IsQuantityComputed(snapshot.SimulatorSourceInfo);
            return _downforceAdapter.GetQuantity(snapshot, carPropertiesDto).GetValueInUnits(ForceUnits);
        }
    }
}
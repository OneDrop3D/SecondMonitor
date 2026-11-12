namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Extractors
{
    using System;
    using Controllers.Settings;
    using DataAdapters;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class TyreLoadHistogramExtractor : AbstractWheelHistogramDataExtractor
    {
        private readonly TyreLoadAdapter _tyreLoadAdapter;

        public TyreLoadHistogramExtractor(ISettingsProvider settingsProvider, TyreLoadAdapter tyreLoadAdapter, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        protected override bool IsZeroBandInMiddle => true;
        public override string YUnit => Force.GetUnitSymbol(ForceUnits);
        public override double DefaultBandSize => Math.Round(Force.GetFromNewtons(100).GetValueInUnits(ForceUnits));
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> WheelValueExtractor => GetTyreLoad;

        private double GetTyreLoad(SimulatorSourceInfo simulatorSourceInfo, WheelInfo wheelInfo, CarPropertiesDto carPropertiesDto)
        {
            return _tyreLoadAdapter.GetQuantityFromWheel(simulatorSourceInfo, wheelInfo, carPropertiesDto).GetValueInUnits(ForceUnits);
        }
    }
}
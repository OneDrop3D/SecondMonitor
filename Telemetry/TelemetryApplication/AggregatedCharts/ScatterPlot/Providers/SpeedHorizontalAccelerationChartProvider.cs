namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class SpeedHorizontalAccelerationChartProvider : AbstractGearsChartProvider
    {
        public SpeedHorizontalAccelerationChartProvider(ILoadedLapsCache loadedLapsCache, SpeedToHorizontalGExtractor speedToHorizontalGExtractor, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(loadedLapsCache, speedToHorizontalGExtractor, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Longitudinal Acceleration (Speed)";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;
    }
}
namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class RpmToHorizontalGChartProvider : AbstractGearsChartProvider
    {
        public RpmToHorizontalGChartProvider(ILoadedLapsCache loadedLapsCache, RpmToHorizontalGExtractor rpmToHorizontalGExtractor, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController)
            : base(loadedLapsCache, rpmToHorizontalGExtractor, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Longitudinal Acceleration (RPM)";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;
    }
}
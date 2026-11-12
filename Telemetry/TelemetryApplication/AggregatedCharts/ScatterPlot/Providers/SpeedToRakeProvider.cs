namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Extractors;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.LoadedLapCache;

    public class SpeedToRakeProvider : AbstractStintScatterPlotProvider
    {
        public SpeedToRakeProvider(SpeedToRakeExtractor dataExtractor, ILoadedLapsCache loadedLapsCache, ISettingsController settingsController, IViewModelFactory viewModelFactory)
            : base(loadedLapsCache, dataExtractor, settingsController, viewModelFactory)
        {
        }

        public override string ChartName => "Rake / Speed";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;
    }
}
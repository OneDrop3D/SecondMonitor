namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Extractors;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.LoadedLapCache;

    public class SpeedToLongGProvider : AbstractStintScatterPlotProvider
    {
        public SpeedToLongGProvider(ILoadedLapsCache loadedLapsCache, SpeedToLongGAllPointsExtractor dataExtractor, ISettingsController settingsController, IViewModelFactory viewModelFactory)
            : base(loadedLapsCache, dataExtractor, settingsController, viewModelFactory)
        {
        }

        public override string ChartName => "Long vs Speed";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;
    }
}